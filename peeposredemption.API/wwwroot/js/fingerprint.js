(async function () {
    try {
        // Canvas fingerprint
        const canvas = document.createElement('canvas');
        canvas.width = 200;
        canvas.height = 50;
        const ctx = canvas.getContext('2d');
        ctx.textBaseline = 'top';
        ctx.font = '14px Arial';
        ctx.fillStyle = '#f60';
        ctx.fillRect(125, 1, 62, 20);
        ctx.fillStyle = '#069';
        ctx.fillText('Torvex fp', 2, 15);
        ctx.fillStyle = 'rgba(102,204,0,0.7)';
        ctx.fillText('Torvex fp', 4, 17);
        const canvasData = canvas.toDataURL();

        // WebGL
        const gl = document.createElement('canvas').getContext('webgl');
        const debugInfo = gl ? gl.getExtension('WEBGL_debug_renderer_info') : null;
        const renderer = debugInfo ? gl.getParameter(debugInfo.UNMASKED_RENDERER_WEBGL) : '';
        const vendor = debugInfo ? gl.getParameter(debugInfo.UNMASKED_VENDOR_WEBGL) : '';

        const components = {
            canvas: canvasData,
            webglRenderer: renderer,
            webglVendor: vendor,
            screenWidth: screen.width,
            screenHeight: screen.height,
            colorDepth: screen.colorDepth,
            timezoneOffset: new Date().getTimezoneOffset(),
            language: navigator.language,
            platform: navigator.platform,
            hardwareConcurrency: navigator.hardwareConcurrency || 0
        };

        const raw = JSON.stringify(components);
        const hashBuffer = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(raw));
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        const hash = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');

        const jwt = document.querySelector('meta[name="jwt"]')?.content;
        if (!jwt) return;

        await fetch('/api/security/fingerprint', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + jwt
            },
            body: JSON.stringify({ fingerprintHash: hash, rawComponents: raw })
        });
    } catch (e) {
        // Silent fail — don't alert the user
    }
})();
