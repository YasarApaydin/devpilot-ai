import React, { useState, useRef } from 'react';
import { Mic, Square, Loader2, AlertCircle } from 'lucide-react';
import type { VoiceState } from '../../hooks/useChat';

interface VoiceRecorderProps {
  voiceState: VoiceState;
  voiceError?: string | null;
  onSendVoice: (audioBlob: Blob, fileName?: string) => void;
  disabled?: boolean;
}

export const VoiceRecorder: React.FC<VoiceRecorderProps> = ({
  voiceState,
  voiceError,
  onSendVoice,
  disabled = false,
}) => {
  const [isRecording, setIsRecording] = useState(false);
  const [recordSeconds, setRecordSeconds] = useState(0);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);
  const timerRef = useRef<any>(null);

  const getExtensionFromMimeType = (mimeType: string): string => {
    const lower = mimeType.toLowerCase();
    if (lower.includes('webm')) return 'webm';
    if (lower.includes('ogg')) return 'ogg';
    if (lower.includes('mp4') || lower.includes('m4a') || lower.includes('aac')) return 'm4a';
    if (lower.includes('wav')) return 'wav';
    return 'webm';
  };

  const startRecording = async () => {
    try {
      audioChunksRef.current = [];
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });

      // Determine browser-supported audio MIME type
      const preferredMime = [
        'audio/webm;codecs=opus',
        'audio/webm',
        'audio/ogg;codecs=opus',
        'audio/mp4',
      ].find((type) => typeof MediaRecorder.isTypeSupported === 'function' && MediaRecorder.isTypeSupported(type));

      const mediaRecorder = preferredMime
        ? new MediaRecorder(stream, { mimeType: preferredMime })
        : new MediaRecorder(stream);

      mediaRecorderRef.current = mediaRecorder;

      mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          audioChunksRef.current.push(event.data);
        }
      };

      mediaRecorder.onstop = () => {
        const actualMimeType = mediaRecorder.mimeType || preferredMime || 'audio/webm';
        const extension = getExtensionFromMimeType(actualMimeType);
        const audioBlob = new Blob(audioChunksRef.current, { type: actualMimeType });
        stream.getTracks().forEach((track) => track.stop());
        onSendVoice(audioBlob, `voice_${Date.now()}.${extension}`);
      };

      mediaRecorder.start();
      setIsRecording(true);
      setRecordSeconds(0);

      timerRef.current = setInterval(() => {
        setRecordSeconds((sec) => sec + 1);
      }, 1000);
    } catch (err) {
      console.error('Microphone access denied or error:', err);
      alert('Could not access microphone. Please ensure microphone permissions are granted.');
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
      setIsRecording(false);
      if (timerRef.current) clearInterval(timerRef.current);
    }
  };

  const isBusy =
    voiceState === 'uploading' ||
    voiceState === 'transcribing' ||
    voiceState === 'processing';

  return (
    <div className="flex items-center gap-2">
      {isRecording ? (
        <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-rose-50 border border-rose-200 text-rose-700 text-xs animate-pulse">
          <div className="w-2 h-2 rounded-full bg-rose-500 animate-ping" />
          <span className="font-mono font-medium text-[11px]">{recordSeconds}s</span>
          <button
            type="button"
            onClick={stopRecording}
            className="w-5 h-5 rounded-full bg-rose-600 hover:bg-rose-700 text-white flex items-center justify-center ml-0.5 transition-transform active:scale-90 cursor-pointer"
            title="Kaydı Durdur ve Gönder"
          >
            <Square className="w-2.5 h-2.5 fill-current" />
          </button>
        </div>
      ) : isBusy ? (
        <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-blue-50 border border-blue-200 text-blue-700 text-xs">
          <Loader2 className="w-3.5 h-3.5 animate-spin text-blue-600" />
          <span className="capitalize text-[11px] font-medium">{voiceState}...</span>
        </div>
      ) : (
        <button
          type="button"
          disabled={disabled}
          onClick={startRecording}
          title="Sesli Sohbet / Ses Kaydı (ChatGPT Ses Modu)"
          className="w-8 h-8 rounded-full flex items-center justify-center bg-slate-100 hover:bg-slate-200 active:scale-95 text-slate-700 transition-all border border-slate-200/80 shadow-2xs disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
        >
          {/* ChatGPT-style voice waveform bars icon */}
          <svg className="w-4 h-4 text-slate-700" viewBox="0 0 24 24" fill="currentColor">
            <rect x="3" y="9" width="2" height="6" rx="1" />
            <rect x="7.5" y="5" width="2" height="14" rx="1" />
            <rect x="12" y="2" width="2" height="20" rx="1" />
            <rect x="16.5" y="5" width="2" height="14" rx="1" />
            <rect x="21" y="9" width="2" height="6" rx="1" />
          </svg>
        </button>
      )}

      {voiceError && (
        <span className="text-[11px] text-rose-600 flex items-center gap-1">
          <AlertCircle className="w-3 h-3" /> {voiceError}
        </span>
      )}
    </div>
  );
};
