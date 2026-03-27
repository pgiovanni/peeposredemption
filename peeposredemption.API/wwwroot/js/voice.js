// ── Voice Channel WebRTC Manager ─────────────────────────────────────
(function () {
    if (typeof isVoiceChannel === 'undefined' || !isVoiceChannel) return;
    if (typeof connection === 'undefined') return;

    const grid = document.getElementById('voice-grid');
    const muteBtn = document.getElementById('vc-mute');
    const deafenBtn = document.getElementById('vc-deafen');
    const cameraBtn = document.getElementById('vc-camera');
    const screenBtn = document.getElementById('vc-screen');
    const disconnectBtn = document.getElementById('vc-disconnect');

    let localStream = null;
    let screenStream = null;
    let iceServers = [];
    const peers = {}; // peerId -> { pc, audioEl, videoEl }
    let isMuted = false;
    let isDeafened = false;
    let isCameraOn = false;
    let isScreenSharing = false;
    let joined = false;

    // Focus state
    let focusedUserId = null;
    let voiceFilmstrip = null;

    function updateGridCount() {
        const inGrid = grid.querySelectorAll('.voice-tile').length;
        const inFilm = voiceFilmstrip ? voiceFilmstrip.querySelectorAll('.voice-tile').length : 0;
        grid.dataset.count = String(inGrid + inFilm);
    }

    function focusTile(userId) {
        if (focusedUserId === userId) { unfocusTile(); return; }
        if (focusedUserId !== null) unfocusTile();

        focusedUserId = userId;
        grid.classList.add('voice-grid--has-focus');

        // Create filmstrip after grid, before controls
        voiceFilmstrip = document.createElement('div');
        voiceFilmstrip.className = 'voice-filmstrip';
        const controls = document.getElementById('voice-controls');
        grid.parentNode.insertBefore(voiceFilmstrip, controls);

        // Move non-focused tiles to filmstrip
        [...grid.querySelectorAll('.voice-tile')].forEach(tile => {
            if (tile.id !== `tile-${userId}`) voiceFilmstrip.appendChild(tile);
        });

        document.getElementById(`tile-${userId}`)?.classList.add('voice-tile--focused');
    }

    function unfocusTile() {
        if (focusedUserId === null) return;
        document.getElementById(`tile-${focusedUserId}`)?.classList.remove('voice-tile--focused');
        if (voiceFilmstrip) {
            [...voiceFilmstrip.querySelectorAll('.voice-tile')].forEach(tile => grid.appendChild(tile));
            voiceFilmstrip.remove();
            voiceFilmstrip = null;
        }
        grid.classList.remove('voice-grid--has-focus');
        focusedUserId = null;
        updateGridCount();
    }

    // Expose voice state globally so other code can detect we're in voice
    window.__voice = { joined: false, channelId: null, channelName: null, leave: null };

    // Fetch ICE server config
    async function fetchIceServers() {
        try {
            const resp = await fetch('/api/ice-servers');
            if (resp.ok) iceServers = await resp.json();
        } catch (_) {}
        if (!iceServers.length) iceServers = [{ urls: 'stun:stun.l.google.com:19302' }];
    }

    // Get user media (audio always, video if camera on)
    async function getLocalStream(withVideo) {
        const constraints = { audio: true, video: withVideo ? { width: 640, height: 480 } : false };
        try {
            const stream = await navigator.mediaDevices.getUserMedia(constraints);
            return stream;
        } catch (err) {
            // Fall back to audio-only if video fails
            if (withVideo) {
                try { return await navigator.mediaDevices.getUserMedia({ audio: true, video: false }); }
                catch (_) {}
            }
            console.error('Failed to get media:', err);
            return null;
        }
    }

    // Create a video/audio tile for a participant
    function createTile(userId, displayName, avatarUrl) {
        const tile = document.createElement('div');
        tile.className = 'voice-tile';
        tile.id = `tile-${userId}`;

        const video = document.createElement('video');
        video.autoplay = true;
        video.playsInline = true;
        video.muted = (userId === currentUserId); // mute own video to prevent echo
        video.className = 'voice-tile-video hidden';
        tile.appendChild(video);

        const avatar = document.createElement('div');
        avatar.className = 'voice-tile-avatar';
        if (avatarUrl) {
            avatar.innerHTML = `<img src="${avatarUrl}" alt="${displayName}" />`;
        } else {
            avatar.textContent = (displayName || '?')[0].toUpperCase();
        }
        tile.appendChild(avatar);

        const nameTag = document.createElement('div');
        nameTag.className = 'voice-tile-name';
        nameTag.textContent = displayName || 'Unknown';
        tile.appendChild(nameTag);

        const statusIcons = document.createElement('div');
        statusIcons.className = 'voice-tile-status';
        tile.appendChild(statusIcons);

        tile.addEventListener('click', () => focusTile(userId));
        grid.appendChild(tile);
        updateGridCount();
        return { tile, video, avatar, nameTag, statusIcons };
    }

    // Polite-peer pattern: lower userId creates offers
    function isPolite(remoteUserId) {
        return currentUserId > remoteUserId;
    }

    function createPeerConnection(remoteUserId) {
        const pc = new RTCPeerConnection({ iceServers });
        const polite = isPolite(remoteUserId);
        let makingOffer = false;
        let ignoreOffer = false;

        // Add local tracks
        if (localStream) {
            localStream.getTracks().forEach(track => pc.addTrack(track, localStream));
        }

        // ICE candidates
        pc.onicecandidate = (e) => {
            if (e.candidate) {
                connection.invoke('SendIceCandidate', remoteUserId, JSON.stringify(e.candidate));
            }
        };

        // Incoming tracks
        pc.ontrack = (e) => {
            const peer = peers[remoteUserId];
            if (!peer) return;

            const stream = e.streams[0] || new MediaStream([e.track]);

            if (e.track.kind === 'video') {
                peer.videoEl.srcObject = stream;
                peer.videoEl.classList.remove('hidden');
                peer.avatarEl.classList.add('hidden');
            } else if (e.track.kind === 'audio') {
                if (!peer.audioEl) {
                    peer.audioEl = document.createElement('audio');
                    peer.audioEl.autoplay = true;
                    document.body.appendChild(peer.audioEl);
                }
                // Use a dedicated stream for the audio element so the AudioContext
                // used in speaking detection doesn't interfere with playback.
                const audioStream = new MediaStream([e.track]);
                peer.audioEl.srcObject = audioStream;
                peer.audioEl.muted = isDeafened;
                peer.audioEl.play().catch(() => {});

                // Speaking detection gets its own stream clone
                setupSpeakingDetection(remoteUserId, new MediaStream([e.track]));
            }
        };

        // Negotiation
        pc.onnegotiationneeded = async () => {
            try {
                makingOffer = true;
                await pc.setLocalDescription();
                connection.invoke('SendWebRtcOffer', remoteUserId, JSON.stringify(pc.localDescription));
            } catch (err) {
                console.error('Negotiation error:', err);
            } finally {
                makingOffer = false;
            }
        };

        pc.onsignalingstatechange = () => {
            // Clean up if needed
        };

        pc.onconnectionstatechange = () => {
            const peer = peers[remoteUserId];
            if (!peer) return;
            if (pc.connectionState === 'disconnected' || pc.connectionState === 'failed') {
                // Hide frozen video, show avatar while waiting for reconnect
                peer.videoEl.classList.add('hidden');
                peer.avatarEl.classList.remove('hidden');
            }
            if (pc.connectionState === 'failed') {
                pc.restartIce();
            }
        };

        return { pc, polite, makingOffer: () => makingOffer, setIgnoreOffer: (v) => { ignoreOffer = v; }, getIgnoreOffer: () => ignoreOffer };
    }

    function setupSpeakingDetection(userId, stream) {
        try {
            const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
            const source = audioCtx.createMediaStreamSource(stream);
            const analyser = audioCtx.createAnalyser();
            analyser.fftSize = 256;
            source.connect(analyser);

            const data = new Uint8Array(analyser.frequencyBinCount);
            let speaking = false;

            function check() {
                if (userId !== currentUserId && !peers[userId]) return; // stop if remote peer removed
                analyser.getByteFrequencyData(data);
                const avg = data.reduce((a, b) => a + b, 0) / data.length;
                const nowSpeaking = avg > 20;

                if (nowSpeaking !== speaking) {
                    speaking = nowSpeaking;
                    const tile = document.getElementById(`tile-${userId}`);
                    if (tile) tile.classList.toggle('speaking', speaking);
                }
                requestAnimationFrame(check);
            }
            check();
        } catch (_) {}
    }

    // ── SignalR voice handlers ──────────────────────────

    connection.on('VoiceParticipantList', (participants) => {
        // Clear grid, reset focus state
        grid.innerHTML = '';
        if (voiceFilmstrip) { voiceFilmstrip.remove(); voiceFilmstrip = null; }
        grid.classList.remove('voice-grid--has-focus');
        focusedUserId = null;
        participants.forEach(p => {
            const tileData = createTile(p.userId, p.displayName, p.avatarUrl);

            if (p.userId === currentUserId) {
                // Local tile — show own video
                if (localStream) {
                    const videoTrack = localStream.getVideoTracks()[0];
                    if (videoTrack) {
                        tileData.video.srcObject = localStream;
                        tileData.video.classList.remove('hidden');
                        tileData.avatar.classList.add('hidden');
                    }
                }
                // Speaking detection for self
                if (localStream) setupSpeakingDetection(currentUserId, localStream);
            } else {
                // Create peer connection for remote users
                const peerData = createPeerConnection(p.userId);
                peers[p.userId] = {
                    pc: peerData.pc,
                    polite: peerData.polite,
                    makingOffer: peerData.makingOffer,
                    setIgnoreOffer: peerData.setIgnoreOffer,
                    getIgnoreOffer: peerData.getIgnoreOffer,
                    videoEl: tileData.video,
                    avatarEl: tileData.avatar,
                    audioEl: null
                };

                // If we're the impolite peer (lower userId), we initiate
                if (!peerData.polite) {
                    // onnegotiationneeded will fire automatically when tracks are added
                }
            }

            updateTileStatus(p.userId, p.isMuted, p.isDeafened, p.isCameraOn);
        });
    });

    connection.on('VoiceUserJoined', (user) => {
        // Clean up any stale peer/tile from a previous connection (e.g. reconnect after crash)
        const stale = peers[user.userId];
        if (stale) {
            stale.pc.close();
            if (stale.audioEl) stale.audioEl.remove();
            delete peers[user.userId];
        }
        const staleT = document.getElementById(`tile-${user.userId}`);
        if (staleT) staleT.remove();

        const tileData = createTile(user.userId, user.displayName, user.avatarUrl);
        const peerData = createPeerConnection(user.userId);
        peers[user.userId] = {
            pc: peerData.pc,
            polite: peerData.polite,
            makingOffer: peerData.makingOffer,
            setIgnoreOffer: peerData.setIgnoreOffer,
            getIgnoreOffer: peerData.getIgnoreOffer,
            videoEl: tileData.video,
            avatarEl: tileData.avatar,
            audioEl: null
        };
        updateTileStatus(user.userId, user.isMuted, user.isDeafened, user.isCameraOn);
    });

    connection.on('VoiceUserLeft', (data) => {
        const peer = peers[data.userId];
        if (peer) {
            peer.pc.close();
            if (peer.audioEl) peer.audioEl.remove();
            delete peers[data.userId];
        }
        // If the focused user left, clear focus first
        if (data.userId === focusedUserId) unfocusTile();
        const tile = document.getElementById(`tile-${data.userId}`);
        if (tile) tile.remove();
        updateGridCount();
    });

    connection.on('ReceiveWebRtcOffer', async (data) => {
        const peer = peers[data.fromUserId];
        if (!peer) return;

        const desc = JSON.parse(data.sdp);
        const offerCollision = (desc.type === 'offer') &&
            (peer.makingOffer() || peer.pc.signalingState !== 'stable');

        peer.setIgnoreOffer(!peer.polite && offerCollision);
        if (peer.getIgnoreOffer()) return;

        await peer.pc.setRemoteDescription(desc);

        if (desc.type === 'offer') {
            await peer.pc.setLocalDescription();
            connection.invoke('SendWebRtcAnswer', data.fromUserId, JSON.stringify(peer.pc.localDescription));
        }
    });

    connection.on('ReceiveWebRtcAnswer', async (data) => {
        const peer = peers[data.fromUserId];
        if (!peer) return;
        const desc = JSON.parse(data.sdp);
        await peer.pc.setRemoteDescription(desc);
    });

    connection.on('ReceiveIceCandidate', async (data) => {
        const peer = peers[data.fromUserId];
        if (!peer) return;
        try {
            const candidate = JSON.parse(data.candidate);
            await peer.pc.addIceCandidate(candidate);
        } catch (err) {
            if (!peer.getIgnoreOffer()) console.error('ICE candidate error:', err);
        }
    });

    connection.on('VoiceStateChanged', (data) => {
        updateTileStatus(data.userId, data.isMuted, data.isDeafened, data.isCameraOn);

        // If someone turned off camera, hide their video
        if (data.isCameraOn === false) {
            const peer = peers[data.userId];
            if (peer) {
                peer.videoEl.classList.add('hidden');
                peer.avatarEl.classList.remove('hidden');
            }
        }
    });

    function updateTileStatus(userId, muted, deafened, cameraOn) {
        const tile = document.getElementById(`tile-${userId}`);
        if (!tile) return;
        const status = tile.querySelector('.voice-tile-status');
        if (!status) return;
        status.innerHTML = '';
        if (muted) status.innerHTML += '<span class="status-icon muted" title="Muted">🔇</span>';
        if (deafened) status.innerHTML += '<span class="status-icon deafened" title="Deafened">🔈</span>';
    }

    // ── Control buttons ──────────────────────────

    muteBtn.addEventListener('click', () => {
        isMuted = !isMuted;
        muteBtn.classList.toggle('active', isMuted);
        if (localStream) {
            localStream.getAudioTracks().forEach(t => t.enabled = !isMuted);
        }
        connection.invoke('UpdateVoiceState', channelId, isMuted, null, null);
    });

    deafenBtn.addEventListener('click', () => {
        isDeafened = !isDeafened;
        deafenBtn.classList.toggle('active', isDeafened);
        // Mute/unmute all remote audio
        Object.values(peers).forEach(p => {
            if (p.audioEl) p.audioEl.muted = isDeafened;
        });
        connection.invoke('UpdateVoiceState', channelId, null, isDeafened, null);
    });

    cameraBtn.addEventListener('click', async () => {
        isCameraOn = !isCameraOn;
        cameraBtn.classList.toggle('active', isCameraOn);

        if (isCameraOn) {
            try {
                const videoStream = await navigator.mediaDevices.getUserMedia({ video: { width: 640, height: 480 } });
                const videoTrack = videoStream.getVideoTracks()[0];

                // Add video track to local stream
                if (localStream) localStream.addTrack(videoTrack);

                // Add to all peer connections
                Object.values(peers).forEach(p => {
                    p.pc.addTrack(videoTrack, localStream);
                });

                // Show own video
                const selfTile = document.getElementById(`tile-${currentUserId}`);
                if (selfTile) {
                    const video = selfTile.querySelector('.voice-tile-video');
                    const avatar = selfTile.querySelector('.voice-tile-avatar');
                    if (video) { video.srcObject = localStream; video.classList.remove('hidden'); }
                    if (avatar) avatar.classList.add('hidden');
                }
            } catch (err) {
                console.error('Camera error:', err);
                isCameraOn = false;
                cameraBtn.classList.remove('active');
            }
        } else {
            // Remove video track
            if (localStream) {
                localStream.getVideoTracks().forEach(t => {
                    t.stop();
                    localStream.removeTrack(t);
                    Object.values(peers).forEach(p => {
                        const sender = p.pc.getSenders().find(s => s.track === t);
                        if (sender) p.pc.removeTrack(sender);
                    });
                });
            }
            // Hide own video
            const selfTile = document.getElementById(`tile-${currentUserId}`);
            if (selfTile) {
                const video = selfTile.querySelector('.voice-tile-video');
                const avatar = selfTile.querySelector('.voice-tile-avatar');
                if (video) { video.srcObject = null; video.classList.add('hidden'); }
                if (avatar) avatar.classList.remove('hidden');
            }
        }
        connection.invoke('UpdateVoiceState', channelId, null, null, isCameraOn);
    });

    screenBtn.addEventListener('click', async () => {
        if (!isScreenSharing) {
            try {
                screenStream = await navigator.mediaDevices.getDisplayMedia({ video: true });
                const screenTrack = screenStream.getVideoTracks()[0];

                // Replace or add screen track in peer connections
                Object.values(peers).forEach(p => {
                    const sender = p.pc.getSenders().find(s => s.track?.kind === 'video');
                    if (sender) {
                        sender.replaceTrack(screenTrack);
                    } else {
                        p.pc.addTrack(screenTrack, screenStream);
                    }
                });

                // Show screen share in own tile
                const selfTile = document.getElementById(`tile-${currentUserId}`);
                if (selfTile) {
                    const video = selfTile.querySelector('.voice-tile-video');
                    const avatar = selfTile.querySelector('.voice-tile-avatar');
                    if (video) { video.srcObject = screenStream; video.classList.remove('hidden'); }
                    if (avatar) avatar.classList.add('hidden');
                }

                isScreenSharing = true;
                screenBtn.classList.add('active');

                // Handle user stopping screen share via browser UI
                screenTrack.onended = () => stopScreenShare();
            } catch (err) {
                console.error('Screen share error:', err);
            }
        } else {
            stopScreenShare();
        }
    });

    function stopScreenShare() {
        if (!screenStream) return;
        screenStream.getTracks().forEach(t => t.stop());

        // Restore camera track or remove video
        const cameraTrack = localStream?.getVideoTracks()[0];
        Object.values(peers).forEach(p => {
            const sender = p.pc.getSenders().find(s => s.track?.kind === 'video');
            if (sender) {
                if (isCameraOn && cameraTrack) {
                    sender.replaceTrack(cameraTrack);
                } else {
                    p.pc.removeTrack(sender);
                }
            }
        });

        const selfTile = document.getElementById(`tile-${currentUserId}`);
        if (selfTile) {
            const video = selfTile.querySelector('.voice-tile-video');
            const avatar = selfTile.querySelector('.voice-tile-avatar');
            if (isCameraOn && localStream) {
                video.srcObject = localStream;
            } else {
                video.srcObject = null;
                video.classList.add('hidden');
                if (avatar) avatar.classList.remove('hidden');
            }
        }

        screenStream = null;
        isScreenSharing = false;
        screenBtn.classList.remove('active');
    }

    disconnectBtn.addEventListener('click', () => {
        leaveVoice();
        // Navigate to a text channel so the page doesn't reload this voice channel and auto-rejoin
        const firstTextChannel = document.querySelector('.channel-item[data-channel-type="0"]');
        window.location.href = firstTextChannel ? firstTextChannel.href : '/App/Index';
    });

    async function leaveVoice() {
        if (!joined) return;
        joined = false;
        window.__voice.joined = false;

        // Close all peer connections
        Object.entries(peers).forEach(([id, p]) => {
            p.pc.close();
            if (p.audioEl) p.audioEl.remove();
        });
        Object.keys(peers).forEach(k => delete peers[k]);

        // Stop local media
        if (localStream) {
            localStream.getTracks().forEach(t => t.stop());
            localStream = null;
        }
        if (screenStream) {
            screenStream.getTracks().forEach(t => t.stop());
            screenStream = null;
        }

        try {
            await connection.invoke('LeaveVoiceChannel', channelId);
        } catch (_) {}
    }

    // Auto-join on page load
    async function joinVoice() {
        await fetchIceServers();
        localStream = await getLocalStream(false);
        if (!localStream) {
            grid.innerHTML = '<div class="voice-error">Could not access microphone. Please grant permission and reload.</div>';
            return;
        }

        // Mute by default if user preference
        localStream.getAudioTracks().forEach(t => t.enabled = !isMuted);

        try {
            // Wait for connection to be ready
            if (connection.state !== 'Connected') {
                await new Promise((resolve) => {
                    const check = setInterval(() => {
                        if (connection.state === 'Connected') { clearInterval(check); resolve(); }
                    }, 100);
                });
            }
            await connection.invoke('JoinVoiceChannel', channelId);
            joined = true;
            window.__voice.joined = true;
            window.__voice.channelId = channelId;
            window.__voice.channelName = (typeof channelName !== 'undefined' ? channelName : '');
        } catch (err) {
            grid.innerHTML = `<div class="voice-error">${err.message || 'Failed to join voice channel.'}</div>`;
        }
    }

    // Leave on page unload
    window.addEventListener('beforeunload', () => {
        if (joined) {
            // Best-effort leave
            navigator.sendBeacon?.('/api/voice-leave'); // Not implemented, rely on OnDisconnectedAsync
            leaveVoice();
        }
    });

    window.__voice.leave = leaveVoice;
    joinVoice();
})();
