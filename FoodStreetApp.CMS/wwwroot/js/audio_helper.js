window.audioHelper = {
    play: function (elementId) {
        const audio = document.getElementById(elementId);
        if (audio) {
            audio.play().catch(err => console.error("Audio playback failed:", err));
        }
    },
    pause: function (elementId) {
        const audio = document.getElementById(elementId);
        if (audio) {
            audio.pause();
        }
    },
    speak: function (text, lang) {
        // Stop any current speech
        window.speechSynthesis.cancel();
        
        if (!text) return;

        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = lang; // e.g., 'en-US', 'vi-VN', 'zh-CN', 'ko-KR', 'ja-JP'
        utterance.rate = 0.9;
        
        window.speechSynthesis.speak(utterance);
    },
    stopSpeaking: function() {
        window.speechSynthesis.cancel();
    }
};
