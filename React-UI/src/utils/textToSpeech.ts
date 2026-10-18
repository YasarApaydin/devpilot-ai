/**
 * Text-to-Speech (TTS) utility using native Web Speech API.
 * Provides clear, natural Turkish voice readout for AI assistant responses.
 */

let activeUtterance: SpeechSynthesisUtterance | null = null;
let currentSpeakingId: string | null = null;

/**
 * Prepares raw AI assistant markdown text for natural, smooth spoken audio.
 * Removes code syntax, raw symbols, markdown tags, and adds natural speech pauses.
 */
function prepareTextForSpeech(markdown: string): string {
  if (!markdown) return '';

  return markdown
    // Replace code blocks with natural audio cue
    .replace(/```[\s\S]*?```/g, ' . Kod bloğu atlandı. ')
    // Replace inline code `xyz` with xyz
    .replace(/`([^`]+)`/g, '$1')
    // Remove markdown headers #, ##, ###
    .replace(/^#+\s+/gm, '')
    // Remove bold and italic markers **text**, *text*
    .replace(/\*\*(.*?)\*\*/g, '$1')
    .replace(/\*(.*?)\*/g, '$1')
    // Remove links [text](url) -> text
    .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
    // Clean bullet list markers - or *
    .replace(/^[-*]\s+/gm, '')
    // Replace HTML tags
    .replace(/<[^>]*>/g, '')
    // Normalize excessive newlines and whitespace
    .replace(/\n+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
}

/**
 * Finds the best available natural Turkish voice on the client system.
 */
function getBestTurkishVoice(): SpeechSynthesisVoice | null {
  if (typeof window === 'undefined' || !('speechSynthesis' in window)) {
    return null;
  }

  const voices = window.speechSynthesis.getVoices();
  if (!voices || voices.length === 0) return null;

  // 1. First priority: Turkish voices with "Natural" or "Google" in name
  const naturalTurkish = voices.find(
    (v) =>
      (v.lang.toLowerCase().startsWith('tr') || v.lang.toLowerCase().includes('tr-tr')) &&
      (v.name.includes('Natural') || v.name.includes('Google') || v.name.includes('Online'))
  );
  if (naturalTurkish) return naturalTurkish;

  // 2. Second priority: Any Turkish voice
  const anyTurkish = voices.find(
    (v) => v.lang.toLowerCase().startsWith('tr') || v.lang.toLowerCase().includes('tr-tr')
  );
  if (anyTurkish) return anyTurkish;

  return null;
}

export const textToSpeech = {
  /**
   * Reads the given message aloud in Turkish.
   * If the same message is already playing, it stops it.
   */
  speak(
    messageId: string,
    text: string,
    onStart?: () => void,
    onEnd?: () => void,
    onError?: (err: any) => void
  ): boolean {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) {
      console.warn('SpeechSynthesis is not supported in this browser.');
      return false;
    }

    // If currently speaking this exact message, stop it (toggle behavior)
    if (currentSpeakingId === messageId && window.speechSynthesis.speaking) {
      this.stop();
      onEnd?.();
      return false;
    }

    // Stop any ongoing speech
    this.stop();

    const spokenText = prepareTextForSpeech(text);
    if (!spokenText) {
      onEnd?.();
      return false;
    }

    const utterance = new SpeechSynthesisUtterance(spokenText);
    utterance.lang = 'tr-TR';
    utterance.rate = 1.0; // Natural conversational speed
    utterance.pitch = 1.0; // Natural pitch

    const voice = getBestTurkishVoice();
    if (voice) {
      utterance.voice = voice;
    }

    utterance.onstart = () => {
      currentSpeakingId = messageId;
      activeUtterance = utterance;
      onStart?.();
    };

    utterance.onend = () => {
      currentSpeakingId = null;
      activeUtterance = null;
      onEnd?.();
    };

    utterance.onerror = (e) => {
      currentSpeakingId = null;
      activeUtterance = null;
      onEnd?.();
      onError?.(e);
    };

    activeUtterance = utterance;
    currentSpeakingId = messageId;

    window.speechSynthesis.speak(utterance);
    return true;
  },

  /**
   * Immediately stops any speech synthesis.
   */
  stop(): void {
    if (typeof window !== 'undefined' && 'speechSynthesis' in window) {
      window.speechSynthesis.cancel();
    }
    currentSpeakingId = null;
    activeUtterance = null;
  },

  /**
   * Returns true if speech synthesis is currently active.
   */
  isSpeaking(): boolean {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) return false;
    return window.speechSynthesis.speaking;
  },

  /**
   * Returns current active message id being spoken.
   */
  getCurrentSpeakingId(): string | null {
    return currentSpeakingId;
  },
};
