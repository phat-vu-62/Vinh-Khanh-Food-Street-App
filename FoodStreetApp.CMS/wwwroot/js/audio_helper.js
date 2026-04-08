window.audioHelper = {
    play: function (element) {
        if (element && typeof element.play === 'function') {
            element.play().catch(err => {
                console.error("Audio playback failed:", err);
                // Attempt to notify Blazor if it's already attached
                if (element._dotNetHelper) {
                    element._dotNetHelper.invokeMethodAsync('OnPlaybackError', err.message);
                }
            });
        }
    },
    pause: function (element) {
        if (element && typeof element.pause === 'function') {
            element.pause();
        }
    },
    attachEvents: function (element, dotNetHelper) {
        if (!element) return;
        
        element._dotNetHelper = dotNetHelper;

        element.onplaying = () => {
            dotNetHelper.invokeMethodAsync('OnPlaybackStateChanged', true);
        };

        element.onpause = () => {
            dotNetHelper.invokeMethodAsync('OnPlaybackStateChanged', false);
        };

        element.onended = () => {
            dotNetHelper.invokeMethodAsync('OnPlaybackStateChanged', false);
        };

        element.onerror = () => {
            const error = element.error ? element.error.message : "Unknown audio error";
            dotNetHelper.invokeMethodAsync('OnPlaybackError', error);
        };

        element.onwaiting = () => {
            dotNetHelper.invokeMethodAsync('OnBuffering', true);
        };

        element.oncanplay = () => {
            dotNetHelper.invokeMethodAsync('OnBuffering', false);
        };
    },
    speak: function (text, lang) {
        window.speechSynthesis.cancel();
        if (!text) return;

        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = lang;
        utterance.rate = 0.9;
        
        utterance.onstart = () => {
            // We could add callbacks here if needed
        };
        
        window.speechSynthesis.speak(utterance);
    },
    stopSpeaking: function() {
        window.speechSynthesis.cancel();
    }
};
