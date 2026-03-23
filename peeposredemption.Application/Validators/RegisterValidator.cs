using FluentValidation;
using peeposredemption.Application.Features.Auth.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.Validators
{

    public class RegisterValidator : AbstractValidator<RegisterCommand>
    {
        private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "mailinator.com", "guerrillamail.com", "guerrillamail.net", "guerrillamail.org",
            "guerrillamail.biz", "guerrillamail.de", "guerrillamail.info", "grr.la",
            "tempmail.com", "temp-mail.org", "throwam.com", "yopmail.com", "yopmail.fr",
            "cool.fr.nf", "jetable.fr.nf", "nospam.ze.tc", "nomail.xl.cx", "mega.zik.dj",
            "speed.1s.fr", "courriel.fr.nf", "moncourrier.fr.nf", "monemail.fr.nf",
            "monmail.fr.nf", "sharklasers.com", "guerrillamailblock.com", "spam4.me",
            "trashmail.at", "trashmail.com", "trashmail.io", "trashmail.me", "trashmail.net",
            "trashmail.org", "trashmail.xyz", "dispostable.com", "mailnull.com",
            "spamgourmet.com", "spamgourmet.net", "spamgourmet.org", "mailexpire.com",
            "throwam.com", "fakeinbox.com", "mailnesia.com", "mailnull.com", "maildrop.cc",
            "spamspot.com", "spamfree24.org", "spam.la", "spamday.com", "spamfree.eu",
            "throwam.com", "discardmail.com", "discardmail.de", "spamgrap.com",
            "mailseal.de", "deadaddress.com", "mailscrap.com", "spambox.us",
            "emailondeck.com", "10minutemail.com", "10minutemail.net", "10minutemail.org",
            "20minutemail.com", "30minutemail.com", "filzmail.com", "spamgap.com",
            "spamhere.com", "spamhole.com", "spaminmotion.com", "spamoff.de",
            "spamtrail.com", "spamtrap.ro", "superrito.com", "sweetxxx.de",
            "tempe-mail.com", "tempemail.com", "tempemail.net", "tempinbox.com",
            "tempymail.com", "thankyou2010.com", "thisisnotmyrealemail.com",
            "throwam.com", "throwaway.email", "throwam.com", "tinoza.org",
            "trbvm.com", "turual.com", "tweetthis.com", "txen.de", "tyldd.com",
            "uggsrock.com", "ufacturing.com", "uggsrock.com", "ukmail.hub.co.uk",
            "uroid.com", "uvumail.com", "valemail.net", "verifymail.win",
            "w3.to", "wetrashed.com", "whatsaas.com", "wh4f.org", "whopy.com",
            "wilemail.com", "willselfdestruct.com", "wmail.cf", "xagloo.co",
            "xagloo.com", "xemaps.com", "xents.com", "xmaily.com", "xoxy.net",
            "xyzfree.net", "yapped.net", "yep.it", "yert.ye.vc", "yogamaven.com",
            "yopmail.com", "yopmail.fr", "youremailsucks.com", "ypmail.webarnak.fr.eu.org",
            "yuurok.com", "z1p.biz", "za.com", "zainmax.net", "zappmail.com",
            "zehnminutenmail.de", "zetmail.com", "zippymail.info", "zoaxe.com",
            "zoemail.net", "zoemail.org", "zomg.info", "zxcv.com", "zxcvbnm.com",
            "zzz.com", "0815.ru", "0wnd.net", "0wnd.org", "10minutemail.de",
            "20mail.it", "20mail.eu", "anonymail.dk", "another.com", "antireg.ru",
            "antispam.de", "bboxmail.com", "beefmilk.com", "binkmail.com",
            "bio-muesli.info", "bladesmail.net", "bofthew.com", "boun.cr",
            "bouncr.com", "breakthru.com", "brefmail.com", "broadbandninja.com",
            "bsnow.net", "bugmenot.com", "bumpymail.com", "byom.de", "casualdx.com",
            "cheatmail.de", "chetasbooks.com", "chickenmail.de", "choco.la",
            "cishmail.com", "cmail.club", "cname.biz", "cool.fr.nf",
            "correo.blogos.net", "courriel.fr.nf", "courrieltemporaire.com",
            "crapmail.org", "cust.in", "d3p.co.uk", "dacoolest.com",
            "dandikmail.com", "dayrep.com", "dcemail.com", "deadfake.com",
            "deagot.com", "deal-maker.com", "despam.it", "devnullmail.com",
            "dharmatel.net", "discard.email", "discardmail.com", "discardmail.de",
            "dmail.kyty.net", "dodgeit.com", "dodgemail.de", "dontreg.com",
            "dontsendmespam.de", "drdrb.com", "dump-email.info", "dumpandfuck.com",
            "dumpmail.de", "dumpyemail.com", "e4ward.com", "easy-trash-mail.com",
            "easytrashmail.com", "einrot.com", "emailias.com", "emailismy.com",
            "emailtemporario.com.br", "emailwarden.com", "emailx.at.hm",
            "emailxfer.com", "emkei.cz", "emkei.gq", "ephemail.net",
            "etranquil.com", "etranquil.net", "etranquil.org", "evopo.com",
            "expressasia.com", "extremail.ru", "eyepaste.com", "fakeinbox.com",
            "fakeinbox.net", "fakeinbox.org", "fakemail.fr", "fakemailz.com",
            "fammix.com", "fansworldwide.de", "fastacura.com", "fastem.com",
            "fastemail.us", "fastemailer.com", "fastest.cc", "fastimap.com",
            "fastinbox.com", "fastmail.cn", "fivemail.de", "fixmail.tk",
            "fizmail.com", "fleckens.hu", "frapmail.com", "freeinbox.email",
            "freemail.ms", "freundin.ru", "front14.org", "fudgerub.com",
            "garliclife.com", "gehensiemirnichtaufdensack.de", "gelitik.in",
            "get-mail.cf", "get-mail.ga", "get-mail.ml", "get-mail.tk",
            "getairmail.com", "getairmail.cf", "getairmail.ga", "getairmail.gq",
            "getairmail.ml", "getairmail.tk", "getmails.eu", "getonemail.com",
            "getonemail.net", "ghosttexter.de", "giantmail.de", "gilbertson.me",
            "girlsundertheinfluence.com", "gishpuppy.com", "gmai.com",
            "gmailandfacebook.com", "gmailni.com", "gmailnew.com", "gmailnew.eu",
            "goemailgo.com", "gorillaswithdirtyarmpits.com", "goplaycool.com",
            "gowikibooks.com", "gowikicampus.com", "gowikicars.com",
            "gowikifilms.com", "gowikigames.com", "gowikimusic.com",
            "gowikinetwork.com", "gowikitravel.com", "gowikitv.com",
            "grandmamail.com", "grandmasmail.com", "great-host.in",
            "greensloth.com", "gsrv.co.uk", "gustr.com",
            "h.mintemail.com", "hailmail.net", "hat-geld.de", "herpderp.nl",
            "hidemail.de", "hirobo2.com", "hochsitze.com", "hopemail.biz",
            "hpc.tw", "ht.cx", "humaility.com", "hurify1.com",
            "ieh-mail.de", "igelonline.de", "ihateyoualot.info", "iheartspam.org",
            "imails.info", "inboxalias.com", "inboxclean.com", "inboxclean.org",
            "incognitomail.com", "incognitomail.net", "incognitomail.org",
            "insorg-mail.info", "instant-mail.de", "internet-e-mail.de",
            "internet-mail.de", "internetemails.net", "internetmailing.net",
            "intheband.net", "intopwa.com", "intracom.net", "inv8r.com",
            "ipoo.org", "ipsur.org", "irc.so", "irish2me.com", "iroid.com",
            "jnxjn.com", "jobbikszimpatizans.hu", "joelpet.com", "junk.to",
            "junkmail.com", "junkmail.gq", "juridicablog.com", "justemail.net",
            "kasmail.com", "kaspop.com", "killmail.com", "killmail.net",
            "klassmaster.com", "klassmaster.net", "klzlk.com", "koszmail.pl",
            "kulturbetrieb.info", "kurzepost.de", "letthemeatspam.com",
            "lhsdv.com", "lifebyfood.com", "link2mail.net", "litedrop.com",
            "llogin.ru", "lol.ovpn.to", "lookugly.com", "lortemail.dk",
            "lroid.com", "m4il.com", "maboard.com", "mail-filter.com",
            "mail-temporaire.fr", "mail.by", "mail.mezimages.net",
            "mail1a.de", "mail21.cc", "mail2rss.org", "mail333.com",
            "mailbidon.com", "mailbiz.biz", "mailblocks.com", "mailbolt.com",
            "mailc.net", "mailcat.biz", "mailcatch.com", "maildrop.cf",
            "maildrop.ga", "maildrop.gq", "maildrop.ml", "maildrop.tk",
            "maileimer.de", "mailexpire.com", "mailfa.tk", "mailforspam.com",
            "mailfreeonline.com", "mailfs.com", "mailguard.me", "mailhazard.com",
            "mailhz.me", "mailimate.com", "mailin8r.com", "mailinater.com",
            "mailinator.net", "mailinator.org", "mailinator.us", "mailinator2.com",
            "mailincubator.com", "mailink.net", "mailite.com", "mailme.gq",
            "mailme.ir", "mailme.lv", "mailme24.com", "mailmetrash.com",
            "mailmoat.com", "mailms.com", "mailnew.com", "mailnull.com",
            "mailorg.org", "mailpick.biz", "mailproxsy.com", "mailquack.com",
            "mailrock.biz", "mailsac.com", "mailscrap.com", "mailseal.de",
            "mailshell.com", "mailsiphon.com", "mailslite.com", "mailswork.com",
            "mailtome.de", "mailtothis.com", "mailtrash.net", "mailtv.net",
            "mailtv.tv", "mailvault.com", "mailw.info", "mailworker.com",
            "mailzilla.com", "mailzilla.org", "mbx.cc", "mega.zik.dj",
            "meltmail.com", "mexicomail.com", "mintemail.com", "misterpinball.de",
            "mjukglass.nu", "mlspc.com", "mockmyid.com", "momentics.ru",
            "moncourrier.fr.nf", "monemail.fr.nf", "monmail.fr.nf",
            "monumentmail.com", "mt2009.com", "mt2014.com", "mt2015.com",
            "myalias.pw", "mymail-in.net", "mymailoasis.com", "mynetstore.de",
            "mypacks.net", "mypartyclip.de", "myphantomemail.com",
            "myspaceinc.com", "myspaceinc.net", "myspaceinc.org",
            "myspacepimpage.com", "mytemp.email", "mytempemail.com",
            "mytempmail.com", "naviox.com", "netzidiot.de", "neverbox.com",
            "niki-stpetersburg.ru", "no-spam.ws", "noclickemail.com",
            "nogmailspam.info", "nomail.pw", "nomail.xl.cx", "nomail2me.com",
            "nomorespamemails.com", "nonspam.eu", "nonspammer.de",
            "noref.in", "norseforce.com", "nospam.ze.tc", "nospam4.us",
            "nospamfor.us", "nospammail.net", "nospamthanks.info",
            "notmailinator.com", "nowmymail.com", "nullbox.info",
            "o2.pl", "objectmail.com", "odaymail.com", "odnorazovoe.ru",
            "ohaaa.de", "omail.pro", "oneoffemail.com", "oneoffmail.com",
            "onewaymail.com", "oopi.org", "opayq.com", "ordinaryamerican.net",
            "otherinbox.com", "ourklips.com", "outlawspam.com",
            "ovpn.to", "owlpic.com", "pancakemail.com", "paplease.com",
            "pepbot.com", "pfui.ru", "phentermine-mortgages.com", "pimpedup.com",
            "pjjkp.com", "plexolan.de", "poczta.onet.pl", "politikerclub.de",
            "poofy.org", "pookmail.com", "proxymail.eu", "prtnx.com",
            "prtz.eu", "pubmail.io", "putthisinyourspamdatabase.com",
            "pwrby.com", "qq.com", "quickinbox.com", "rcpt.at",
            "recode.me", "recursor.net", "recyclemail.dk", "regbypass.com",
            "regbypass.comsafe-mail.net", "rejectmail.com", "relitech.nl",
            "reppui.com", "reversecards.com", "rklips.com", "rmqkr.net",
            "rootfest.net", "rppkn.com", "rtrtr.com", "s0ny.net",
            "safe-mail.net", "safetymail.info", "safetypost.de", "sandelf.de",
            "saynotospams.com", "schafmail.de", "schrott-email.de",
            "secretemail.de", "secure-mail.biz", "selfdestructingmail.com",
            "sendingspecialflyers.com", "senr.net", "services391.com",
            "sharklasers.com", "sharedmailbox.org", "shieldedmail.com",
            "shitmail.de", "shitmail.me", "shitmail.org", "shortmail.net",
            "sibmail.com", "sinnlos-mail.de", "slopsbox.com", "slushmail.com",
            "snakemail.com", "sneakemail.com", "sneakmail.de", "snkmail.com",
            "sofimail.com", "sofort-mail.de", "sogetthis.com", "soioa.com",
            "soisz.com", "spam.la", "spam.org.tr", "spam.su", "spam4.me",
            "spamavert.com", "spambob.com", "spambob.net", "spambob.org",
            "spambog.com", "spambog.de", "spambog.ru", "spambox.info",
            "spambox.irishspringrealty.com", "spambox.us", "spamcannon.com",
            "spamcannon.net", "spamcero.com", "spamcon.org", "spamcorptastic.com",
            "spamcowboy.com", "spamcowboy.net", "spamcowboy.org",
            "spamday.com", "spamdecoy.net", "spameater.com", "spameater.org",
            "spamex.com", "spamfree.eu", "spamfree24.org",
            "spamgoes.in", "spamgourmet.com", "spamgourmet.net",
            "spamgourmet.org", "spamgrap.com", "spamherelots.com",
            "spamhereplease.com", "spamhole.com", "spamify.com",
            "spaminator.de", "spamkill.info", "spaml.com", "spaml.de",
            "spammotel.com", "spamobox.com", "spamoff.de", "spampast.com",
            "spampontoon.com", "spamrock.com", "spamslicer.com",
            "spamspot.com", "spamstack.net", "spamthis.co.uk",
            "spamthisplease.com", "spamtrap.ro", "spamtraps.net",
            "spamur.com", "spamurl.fun", "spamwc.cf", "spamwc.de",
            "spamwc.ga", "spamwc.gq", "spamwc.ml", "spamwc.net",
            "spamwc.tk", "speed.1s.fr", "spoofmail.de", "stuffmail.de",
            "super-auswahl.de", "supergreatmail.com", "supermailer.jp",
            "superrito.com", "superstachel.de", "suremail.info",
            "svk.jp", "sweetxxx.de", "tafmail.com", "tagyourself.com",
            "tapchicuoihoi.com", "temp-mail.de", "temp-mail.io", "temp-mail.ru",
            "temp.emeraldwebmail.com", "temp.headstrong.de", "tempail.com",
            "tempalias.com", "tempe-mail.com", "tempemail.biz",
            "tempemail.co.za", "tempemail.com", "tempemail.net",
            "tempinbox.co.uk", "tempinbox.com", "tempmail.de",
            "tempmail.eu", "tempmail.it", "tempmail2.com",
            "tempomail.fr", "temporaryemail.net", "temporaryemail.us",
            "temporaryforwarding.com", "temporaryinbox.com",
            "temporarymailaddress.com", "throwam.com",
        };

        public RegisterValidator()
        {
            RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(32);
            RuleFor(x => x.Email).NotEmpty().EmailAddress()
                .Must(email =>
                {
                    var atIdx = email?.IndexOf('@') ?? -1;
                    if (atIdx < 0) return true; // let EmailAddress rule handle it
                    var domain = email!.Substring(atIdx + 1).ToLowerInvariant();
                    if (DisposableDomains.Contains(domain)) return false;
                    // Also block subdomains of blocked domains (e.g. sub.mailinator.com)
                    var dotIdx = domain.IndexOf('.');
                    if (dotIdx >= 0)
                    {
                        var parentDomain = domain.Substring(dotIdx + 1);
                        if (DisposableDomains.Contains(parentDomain)) return false;
                    }
                    return true;
                })
                .WithMessage("Please use a permanent email address to register.");
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
            RuleFor(x => x.DateOfBirth).NotEmpty()
                .WithMessage("Date of birth is required.")
                .Must(dob => dob.HasValue && dob.Value.AddYears(13) <= DateTime.UtcNow)
                .WithMessage("You must be at least 13 years old to create an account.");
        }
    }
}
