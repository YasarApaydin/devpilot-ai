import React, { useState, useEffect } from 'react';
import { Copy, Check, Bot, User, Mic, Volume2, VolumeX, AlertCircle, Cpu } from 'lucide-react';
import { formatTime } from '../../utils/formatDate';
import { textToSpeech } from '../../utils/textToSpeech';
import { PdfButton } from './PdfButton';
import { AudioPlayer } from './AudioPlayer';
import { SecurityScanCard } from '../security/SecurityScanCard';
import type { MessageRole, InputType, SecurityScan } from '../../types';

export interface MessageBubbleProps {
  id?: string;
  role: MessageRole;
  content: string;
  createdAt: string;
  inputType?: InputType;
  isStreaming?: boolean;
  audioUrl?: string;
  userPrompt?: string;
  securityScan?: SecurityScan;
  modelUsed?: string;
  fallbackReason?: string;
  originalModel?: string;
  onScanUpdated?: (scan: SecurityScan) => void;
}

export const MessageBubble: React.FC<MessageBubbleProps> = ({
  id,
  role,
  content,
  createdAt,
  inputType = 'text',
  isStreaming = false,
  audioUrl,
  userPrompt,
  securityScan,
  modelUsed,
  fallbackReason,
  originalModel,
  onScanUpdated,
}) => {
  const [copied, setCopied] = useState(false);
  const [isSpeaking, setIsSpeaking] = useState(false);
  const isUser = role === 'user';

  // Stop speech if unmounted
  useEffect(() => {
    return () => {
      if (isSpeaking) {
        textToSpeech.stop();
      }
    };
  }, [isSpeaking]);

  const copyToClipboard = () => {
    navigator.clipboard.writeText(content);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleToggleSpeech = () => {
    if (isSpeaking) {
      textToSpeech.stop();
      setIsSpeaking(false);
    } else if (id && content) {
      textToSpeech.speak(
        id,
        content,
        () => setIsSpeaking(true),
        () => setIsSpeaking(false),
        () => setIsSpeaking(false)
      );
    }
  };

  // Render inline bold (**bold**) and inline code (`code`)
  const renderInlineTokens = (text: string) => {
    const tokens = text.split(/(`[^`]+`|\*\*[^*]+\*\*)/g);
    return tokens.map((token, idx) => {
      if (token.startsWith('`') && token.endsWith('`') && token.length > 2) {
        return (
          <code
            key={idx}
            className="mx-0.5 px-1.5 py-0.5 rounded-md font-mono text-[11px] bg-slate-100 text-pink-600 border border-slate-200"
          >
            {token.slice(1, -1)}
          </code>
        );
      }
      if (token.startsWith('**') && token.endsWith('**') && token.length > 4) {
        return (
          <strong key={idx} className="font-semibold text-inherit">
            {token.slice(2, -2)}
          </strong>
        );
      }
      return token;
    });
  };

  const renderMarkdownTextSegment = (segment: string, segKey: number) => {
    const lines = segment.split('\n');
    const elements: React.ReactNode[] = [];
    let i = 0;

    while (i < lines.length) {
      const line = lines[i];
      const trimmed = line.trim();

      // Markdown table block detection
      if (trimmed.startsWith('|') && trimmed.endsWith('|')) {
        const tableRows: string[][] = [];
        while (i < lines.length && lines[i].trim().startsWith('|') && lines[i].trim().endsWith('|')) {
          const rowLine = lines[i].trim();
          if (!/^\|[\s:|-]+\|$/.test(rowLine)) {
            const cells = rowLine
              .slice(1, -1)
              .split('|')
              .map((c) => c.trim());
            tableRows.push(cells);
          }
          i++;
        }

        if (tableRows.length > 0) {
          const headerRow = tableRows[0];
          const bodyRows = tableRows.slice(1);
          elements.push(
            <div key={`tbl-${segKey}-${i}`} className="my-2.5 overflow-x-auto rounded-xl border border-slate-200">
              <table className="w-full text-left border-collapse text-xs">
                <thead className="bg-slate-50 border-b border-slate-200 text-slate-700 font-semibold">
                  <tr>
                    {headerRow.map((h, hIdx) => (
                      <th key={hIdx} className="px-3 py-2">
                        {renderInlineTokens(h)}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 bg-white">
                  {bodyRows.map((r, rIdx) => (
                    <tr key={rIdx} className="hover:bg-slate-50/70">
                      {r.map((c, cIdx) => (
                        <td key={cIdx} className="px-3 py-1.5 text-slate-700">
                          {renderInlineTokens(c)}
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          );
        }
        continue;
      }

      if (!trimmed) {
        elements.push(<div key={`sp-${segKey}-${i}`} className="h-2" />);
        i++;
        continue;
      }

      if (trimmed === '---' || trimmed === '***') {
        elements.push(<hr key={`hr-${segKey}-${i}`} className="my-2.5 border-slate-200" />);
        i++;
        continue;
      }

      if (trimmed.startsWith('### ')) {
        elements.push(
          <h4 key={`h3-${segKey}-${i}`} className="font-bold text-slate-900 text-xs sm:text-sm mt-2.5 mb-1">
            {renderInlineTokens(trimmed.slice(4))}
          </h4>
        );
        i++;
        continue;
      }

      if (trimmed.startsWith('## ')) {
        elements.push(
          <h3 key={`h2-${segKey}-${i}`} className="font-bold text-slate-900 text-sm sm:text-base mt-3 mb-1.5">
            {renderInlineTokens(trimmed.slice(3))}
          </h3>
        );
        i++;
        continue;
      }

      if (trimmed.startsWith('# ')) {
        elements.push(
          <h2 key={`h1-${segKey}-${i}`} className="font-extrabold text-slate-900 text-sm sm:text-base mt-2 mb-1.5">
            {renderInlineTokens(trimmed.slice(2))}
          </h2>
        );
        i++;
        continue;
      }

      if (trimmed.startsWith('> ')) {
        elements.push(
          <blockquote
            key={`bq-${segKey}-${i}`}
            className="my-2 pl-3 py-1.5 pr-2.5 border-l-3 border-blue-500 bg-slate-50 rounded-r-lg text-slate-600 text-[11px] sm:text-xs leading-relaxed"
          >
            {renderInlineTokens(trimmed.slice(2))}
          </blockquote>
        );
        i++;
        continue;
      }

      if (trimmed.startsWith('- ') || trimmed.startsWith('* ') || trimmed.startsWith('• ')) {
        const itemText = trimmed.replace(/^[-*•]\s+/, '');
        elements.push(
          <div key={`li-${segKey}-${i}`} className="flex items-start gap-2 my-0.5 text-slate-700 leading-relaxed text-xs sm:text-sm">
            <span className="text-blue-500 font-bold select-none shrink-0 mt-0.5">•</span>
            <div className="flex-1 min-w-0">{renderInlineTokens(itemText)}</div>
          </div>
        );
        i++;
        continue;
      }

      elements.push(
        <div key={`ln-${segKey}-${i}`} className="leading-relaxed">
          {renderInlineTokens(line)}
        </div>
      );
      i++;
    }

    return <div key={segKey} className="space-y-0.5">{elements}</div>;
  };

  // Helper to format code blocks vs regular text
  const renderFormattedContent = (rawText: string) => {
    if (!rawText) return null;

    let textToProcess = rawText;

    // Check for open streaming code block
    const fenceCount = (textToProcess.match(/```/g) || []).length;
    if (isStreaming && fenceCount % 2 === 1) {
      // Append temporary virtual fence so code block is styled dark immediately while streaming
      textToProcess = textToProcess + '\n```';
    }

    const parts = textToProcess.split(/(```[\s\S]*?```)/g);

    return parts.map((part, index) => {
      if (part.startsWith('```') && part.endsWith('```')) {
        const lines = part.slice(3, -3).trim().split('\n');
        const language = lines[0].match(/^[a-zA-Z0-9_-]+$/) ? lines[0] : '';
        const code = language ? lines.slice(1).join('\n') : lines.join('\n');

        return (
          <div
            key={index}
            className="my-3 rounded-xl overflow-hidden bg-[#1e293b] text-slate-100 font-mono text-xs border border-slate-700/60 shadow-xs"
          >
            <div className="flex items-center justify-between px-3.5 py-1.5 bg-slate-800/90 border-b border-slate-700/60 text-[11px] text-slate-300">
              <span className="font-semibold text-slate-300 tracking-wide">{language || 'kod'}</span>
              <button
                type="button"
                onClick={() => {
                  navigator.clipboard.writeText(code);
                }}
                className="hover:text-white flex items-center gap-1 transition-colors px-1.5 py-0.5 rounded hover:bg-slate-700/60 cursor-pointer"
              >
                <Copy className="w-3 h-3" /> Kopyala
              </button>
            </div>
            <pre className="p-3.5 overflow-x-auto leading-relaxed text-slate-100 bg-[#1e293b]">{code}</pre>
          </div>
        );
      }

      if (isUser) {
        return (
          <span key={index} className="whitespace-pre-wrap leading-relaxed">
            {renderInlineTokens(part)}
          </span>
        );
      }

      return renderMarkdownTextSegment(part, index);
    });
  };

  return (
    <div
      className={`flex items-start gap-3 my-4 group w-full min-w-0 animate-slide-up ${
        isUser ? 'flex-row-reverse' : 'flex-row'
      }`}
    >
      {/* Avatar */}
      {isUser ? (
        <div className="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 shadow-xs bg-blue-600 text-white">
          <User className="w-4 h-4" />
        </div>
      ) : (
        <img
          src="/app-logo.png"
          alt="DevPilot Agent"
          className="w-8 h-8 rounded-full object-cover shrink-0 shadow-xs border border-slate-200"
        />
      )}

      {/* Bubble Content */}
      <div className={`flex flex-col min-w-0 max-w-[85%] sm:max-w-[80%] ${isUser ? 'items-end' : 'items-start'}`}>
        <div className="flex items-center gap-2 mb-1 text-[11px] text-slate-500">
          <span className="font-semibold text-slate-700">
            {isUser ? 'You' : 'DevPilot Agent'}
          </span>
          {inputType === 'voice' && (
            <span className="inline-flex items-center gap-0.5 px-1.5 py-0.2 rounded bg-purple-50 border border-purple-200 text-purple-700 text-[10px]">
              <Mic className="w-2.5 h-2.5" /> Voice
            </span>
          )}
          <span>{formatTime(createdAt)}</span>
        </div>

        <div
          className={`p-4 rounded-2xl text-xs sm:text-sm transition-all overflow-hidden break-words w-fit max-w-full ${
            isUser
              ? 'bg-blue-600 text-white rounded-tr-xs shadow-xs'
              : 'bg-white border border-slate-200 text-slate-800 rounded-tl-xs shadow-xs'
          }`}
        >
          <div className="break-words overflow-hidden">
            {renderFormattedContent(content)}
            {isStreaming && (
              <span className="inline-block w-1.5 h-4 ml-1 bg-blue-600 animate-pulse align-middle" />
            )}
          </div>

          {audioUrl && (
            <div className="mt-3 pt-2 border-t border-slate-100">
              <AudioPlayer audioUrl={audioUrl} />
            </div>
          )}

          {securityScan && (
            <div className="mt-2 w-full">
              <SecurityScanCard scan={securityScan} onScanUpdated={onScanUpdated} />
            </div>
          )}
        </div>

        {/* Model Information & Fallback Banner */}
        {!isUser && !isStreaming && (modelUsed || fallbackReason) && (
          <div className="mt-1.5 flex flex-col gap-1 w-full text-[11px]">
            {fallbackReason && (
              <div className="flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg bg-amber-50 border border-amber-200 text-amber-900 font-medium shadow-2xs">
                <AlertCircle className="w-3.5 h-3.5 text-amber-600 shrink-0" />
                <span>
                  {originalModel ? `Seçilen ${originalModel}` : 'Seçilen model'} yanıt veremedi ({fallbackReason}) → DevPilot Varsayılan Modeli ile yanıtlandı.
                </span>
              </div>
            )}
            {modelUsed && (
              <div className="flex items-center gap-1.5 text-slate-500 font-mono text-[10px]">
                <Cpu className="w-3 h-3 text-slate-400 shrink-0" />
                <span>Model: <strong className="text-slate-700 font-semibold">{modelUsed}</strong></span>
              </div>
            )}
          </div>
        )}

        {/* User Message Actions (Copy) */}
        {isUser && content && (
          <div className="flex items-center gap-1.5 mt-1">
            <button
              type="button"
              onClick={copyToClipboard}
              className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-medium text-slate-500 hover:text-slate-800 hover:bg-slate-200/60 transition-colors cursor-pointer"
              title="Promptu kopyala"
            >
              {copied ? (
                <>
                  <Check className="w-3 h-3 text-emerald-600" />
                  <span className="text-emerald-700 font-semibold">Kopyalandı</span>
                </>
              ) : (
                <>
                  <Copy className="w-3 h-3 text-slate-400" />
                  <span>Kopyala</span>
                </>
              )}
            </button>
          </div>
        )}

        {/* Assistant Message Actions (Copy, Sesli Dinle, PDF) */}
        {!isUser && !isStreaming && content && (
          <div className="flex items-center gap-1.5 mt-1.5 flex-wrap">
            <button
              onClick={copyToClipboard}
              className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-medium text-slate-600 hover:text-slate-900 hover:bg-slate-100 border border-slate-200 bg-white transition-colors shadow-xs cursor-pointer"
              title="Cevabı kopyala"
            >
              {copied ? (
                <>
                  <Check className="w-3.5 h-3.5 text-emerald-600" />
                  <span className="text-emerald-700 font-semibold">Kopyalandı</span>
                </>
              ) : (
                <>
                  <Copy className="w-3.5 h-3.5" />
                  <span>Kopyala</span>
                </>
              )}
            </button>

            {/* Sesli Dinle Butonu */}
            <button
              type="button"
              onClick={handleToggleSpeech}
              className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-medium border transition-colors shadow-xs cursor-pointer ${
                isSpeaking
                  ? 'bg-rose-50 text-rose-700 border-rose-200 animate-pulse'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100 border border-slate-200 bg-white'
              }`}
              title={isSpeaking ? 'Sesli okumayı durdur' : 'Cevabı sesli olarak dinle'}
            >
              {isSpeaking ? (
                <>
                  <VolumeX className="w-3.5 h-3.5 text-rose-600" />
                  <span className="font-semibold text-rose-700">Durdur</span>
                </>
              ) : (
                <>
                  <Volume2 className="w-3.5 h-3.5 text-blue-600" />
                  <span>Sesli Dinle</span>
                </>
              )}
            </button>

            {id && (
              <PdfButton
                type="message"
                id={id}
                label="PDF İndir"
                content={content}
                userPrompt={userPrompt}
                modelUsed={modelUsed}
                fallbackReason={fallbackReason}
                originalModel={originalModel}
              />
            )}
          </div>
        )}
      </div>
    </div>
  );
};
