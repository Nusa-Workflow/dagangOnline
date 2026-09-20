/**
 * NVIDIA Nemotron Voice & Speech Synthesis Player
 * Solves browser autoplay restrictions, integrates Web Speech API (SpeechSynthesis)
 * with base64 WAV stream playback, and provides instant barge-in interruption.
 */
(function () {
    window.currentNemotronAudio = null;
    window.currentNemotronUtterance = null;
    window.isNemotronVoicePlaying = false;

    // Preload available voices
    if ('speechSynthesis' in window) {
        window.speechSynthesis.onvoiceschanged = function () {
            window.nemotronAvailableVoices = window.speechSynthesis.getVoices();
        };
    }

    /**
     * Main Voice Playback Function
     * @param {string} base64Wav - Base64 encoded WAV PCM stream
     * @param {string} spokenText - Spoken text dialogue/script for speech synthesis
     * @param {string} lang - Language code ('id', 'su', 'jv', 'en', etc.)
     */
    window.playNemotronVoice = function (base64Wav, spokenText, lang) {
        console.log("[Nemotron Voice] Request to play voice:", { hasAudio: !!base64Wav, textLen: spokenText ? spokenText.length : 0, lang: lang });
        window.stopNemotronVoice();

        var textToSpeak = spokenText;
        if (textToSpeak) {
            // Clean markdown formatting, tags, links
            textToSpeak = textToSpeak
                .replace(/```[\s\S]*?```/g, '')
                .replace(/\[([^\]]+)\]\([^\)]+\)/g, '$1')
                .replace(/[\*~`#_>]/g, '')
                .replace(/https?:\/\/\S+/g, '')
                .replace(/\s+/g, ' ')
                .trim();
        }

        var playedViaSpeech = false;

        // 1. Web Speech API (speechSynthesis) - Produces crystal clear spoken words in user's speakers
        if ('speechSynthesis' in window && textToSpeak && textToSpeak.length > 0) {
            try {
                window.speechSynthesis.cancel(); // Clear any pending queue
                var utterance = new SpeechSynthesisUtterance(textToSpeak);
                var voices = window.speechSynthesis.getVoices();

                var targetLang = (lang || 'id').toLowerCase();
                var voiceLang = 'id-ID';
                if (targetLang.startsWith('en')) voiceLang = 'en-US';

                utterance.lang = voiceLang;
                utterance.rate = 1.0;
                utterance.pitch = 1.0;

                // Pick an Indonesian or appropriate voice
                if (voices && voices.length > 0) {
                    var matchedVoice = voices.find(function (v) {
                        return v.lang && (v.lang.toLowerCase().startsWith('id') || v.lang.toLowerCase().includes('indonesia'));
                    });
                    if (!matchedVoice && targetLang.startsWith('en')) {
                        matchedVoice = voices.find(function (v) {
                            return v.lang && v.lang.toLowerCase().startsWith('en');
                        });
                    }
                    if (matchedVoice) {
                        utterance.voice = matchedVoice;
                    }
                }

                utterance.onstart = function () {
                    window.isNemotronVoicePlaying = true;
                    showVoicePlayingUI(true);
                    console.log("[Nemotron Voice] Spoken voice started successfully.");
                };

                utterance.onend = function () {
                    window.isNemotronVoicePlaying = false;
                    showVoicePlayingUI(false);
                    console.log("[Nemotron Voice] Spoken voice finished.");
                };

                utterance.onerror = function (e) {
                    console.warn("[Nemotron Voice] SpeechSynthesis error, fallback to audio stream:", e);
                    window.isNemotronVoicePlaying = false;
                    showVoicePlayingUI(false);
                    playRawAudioStream(base64Wav);
                };

                window.currentNemotronUtterance = utterance;
                window.speechSynthesis.speak(utterance);
                playedViaSpeech = true;
            } catch (err) {
                console.warn("[Nemotron Voice] SpeechSynthesis exception:", err);
            }
        }

        // 2. Play base64 WAV PCM container if speech synthesis is not supported or as audio buffer
        if (!playedViaSpeech && base64Wav) {
            playRawAudioStream(base64Wav);
        }
    };

    function playRawAudioStream(base64Wav) {
        if (!base64Wav) return;
        try {
            var audio = new Audio("data:audio/wav;base64," + base64Wav);
            window.currentNemotronAudio = audio;
            audio.onplay = function () {
                window.isNemotronVoicePlaying = true;
                showVoicePlayingUI(true);
            };
            audio.onended = function () {
                window.isNemotronVoicePlaying = false;
                showVoicePlayingUI(false);
            };
            audio.onerror = function (err) {
                console.warn("[Nemotron Voice] Audio element playback error:", err);
                showVoicePlayingUI(false);
            };

            var playPromise = audio.play();
            if (playPromise !== undefined) {
                playPromise.catch(function (err) {
                    console.warn("[Nemotron Voice] Autoplay blocked by browser:", err);
                    showVoicePlayingUI(false);
                });
            }
        } catch (e) {
            console.error("[Nemotron Voice] Failed to instantiate audio:", e);
        }
    }

    /**
     * Stop and Barge-In Voice Playback Instantly
     */
    window.stopNemotronVoice = function () {
        console.log("[Nemotron Voice] Stopping voice / Barge-In triggered.");
        if ('speechSynthesis' in window) {
            window.speechSynthesis.cancel();
        }
        if (window.currentNemotronAudio) {
            window.currentNemotronAudio.pause();
            window.currentNemotronAudio.currentTime = 0;
            window.currentNemotronAudio = null;
        }
        window.currentNemotronUtterance = null;
        window.isNemotronVoicePlaying = false;
        showVoicePlayingUI(false);
    };

    function showVoicePlayingUI(isPlaying) {
        var banner = document.getElementById("nemotronVoicePlayingBanner");
        if (banner) {
            banner.style.display = isPlaying ? "flex" : "none";
        }
    }
})();
