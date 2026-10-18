import React, { useState, useRef, useEffect } from 'react';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';
import { useAuth } from '../../context/AuthContext';
import { feedbackApi } from '../../api/feedbackApi';
import {
  MessageSquarePlus,
  Bug,
  Lightbulb,
  Sparkles,
  HelpCircle,
  Paperclip,
  X,
  FileText,
  Image as ImageIcon,
  CheckCircle2,
  AlertCircle,
  Send,
  Lock,
  ShieldCheck,
} from 'lucide-react';

export interface FeedbackModalProps {
  isOpen: boolean;
  onClose: () => void;
}

const CATEGORIES = [
  {
    id: 'bug',
    label: 'Hata / Bug',
    icon: Bug,
    color: 'text-rose-500 bg-rose-50 border-rose-200 hover:border-rose-300',
    activeColor: 'bg-rose-500 text-white border-rose-600 shadow-xs',
  },
  {
    id: 'feature',
    label: 'Öneri & Özellik',
    icon: Lightbulb,
    color: 'text-amber-500 bg-amber-50 border-amber-200 hover:border-amber-300',
    activeColor: 'bg-amber-500 text-white border-amber-600 shadow-xs',
  },
  {
    id: 'feedback',
    label: 'Görüş & Fikir',
    icon: Sparkles,
    color: 'text-blue-500 bg-blue-50 border-blue-200 hover:border-blue-300',
    activeColor: 'bg-blue-600 text-white border-blue-700 shadow-xs',
  },
  {
    id: 'support',
    label: 'Soru & Destek',
    icon: HelpCircle,
    color: 'text-emerald-500 bg-emerald-50 border-emerald-200 hover:border-emerald-300',
    activeColor: 'bg-emerald-600 text-white border-emerald-700 shadow-xs',
  },
];

const MAX_FILE_SIZE_BYTES = 15 * 1024 * 1024; // 15 MB

export const FeedbackModal: React.FC<FeedbackModalProps> = ({ isOpen, onClose }) => {
  const { user } = useAuth();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [category, setCategory] = useState<string>('bug');
  const [name, setName] = useState<string>('');
  const [email, setEmail] = useState<string>('');
  const [message, setMessage] = useState<string>('');
  const [file, setFile] = useState<File | null>(null);
  const [filePreview, setFilePreview] = useState<string | null>(null);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  // Initialize prefilled info from logged-in user
  useEffect(() => {
    if (isOpen) {
      if (user) {
        const fullName = `${user.firstName || ''} ${user.lastName || ''}`.trim();
        setName(fullName || user.email?.split('@')[0] || '');
        setEmail(user.email || '');
      }
      setErrorMessage(null);
      setIsSuccess(false);
    }
  }, [isOpen, user]);

  // Clean up object URL when preview changes or unmounts
  useEffect(() => {
    return () => {
      if (filePreview) {
        URL.revokeObjectURL(filePreview);
      }
    };
  }, [filePreview]);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0];
    if (!selected) return;

    if (selected.size > MAX_FILE_SIZE_BYTES) {
      setErrorMessage('Dosya boyutu 15 MB sınırını aşıyor. Lütfen daha küçük bir dosya seçin.');
      return;
    }

    setErrorMessage(null);
    setFile(selected);

    if (selected.type.startsWith('image/')) {
      const url = URL.createObjectURL(selected);
      setFilePreview(url);
    } else {
      setFilePreview(null);
    }
  };

  const removeFile = () => {
    if (filePreview) {
      URL.revokeObjectURL(filePreview);
      setFilePreview(null);
    }
    setFile(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!message.trim()) {
      setErrorMessage('Lütfen mesajınızı veya hata detaylarını girin.');
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);

    try {
      const response = await feedbackApi.sendFeedback({
        category,
        message: message.trim(),
        name: name.trim() || undefined,
        email: email.trim() || undefined,
        file: file || undefined,
      });

      if (response && response.success === false) {
        throw new Error(response.message || 'Geri bildirim gönderilemedi.');
      }

      setIsSuccess(true);
      setMessage('');
      removeFile();
    } catch (err: any) {
      const msg = err.response?.data?.message || err.message || 'Gönderim sırasında bir hata oluştu. Lütfen tekrar deneyin.';
      setErrorMessage(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleResetAndClose = () => {
    setIsSuccess(false);
    setErrorMessage(null);
    onClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleResetAndClose}
      title=""
      maxWidth="lg"
    >
      {isSuccess ? (
        <div className="py-6 px-2 text-center space-y-4">
          <div className="w-16 h-16 rounded-2xl bg-emerald-100 text-emerald-600 flex items-center justify-center mx-auto shadow-xs">
            <CheckCircle2 className="w-9 h-9" />
          </div>
          <div>
            <h3 className="text-lg font-bold text-slate-900">
              Geri Bildiriminiz Başarıyla İletildi!
            </h3>
            <p className="text-sm text-slate-600 mt-2 max-w-md mx-auto">
              Mesajınız ve ekli belgeleriniz doğrudan Geliştirici &amp; Sistem Mimarı{' '}
              <strong className="text-slate-900">Yaşar Apaydın</strong>'a ulaştırıldı.
              DevPilot AI'ı geliştirmemize katkı sağladığınız için teşekkür ederiz.
            </p>
          </div>

          <div className="pt-4 flex justify-center">
            <Button
              variant="primary"
              size="md"
              onClick={handleResetAndClose}
              className="px-6"
            >
              Tamam
            </Button>
          </div>
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-4 -mt-1">
          {/* Header */}
          <div className="flex items-center gap-3 pb-3 border-b border-slate-100">
            <div className="w-10 h-10 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center shrink-0 border border-blue-100">
              <MessageSquarePlus className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">
                Kullanıcı Görüşleri &amp; Hata Bildir
              </h3>
              <p className="text-xs text-slate-500">
                Hata ekran görüntüleri, log dosyaları veya yeni fikirlerinizi doğrudan iletin.
              </p>
            </div>
          </div>

          {/* Category Chips */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Bildirim Türü
            </label>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
              {CATEGORIES.map((cat) => {
                const Icon = cat.icon;
                const isSelected = category === cat.id;
                return (
                  <button
                    key={cat.id}
                    type="button"
                    onClick={() => setCategory(cat.id)}
                    className={`flex items-center justify-center gap-1.5 py-2 px-2.5 rounded-xl border text-xs font-medium transition-all cursor-pointer ${
                      isSelected
                        ? cat.activeColor
                        : `${cat.color} hover:bg-slate-50 text-slate-700`
                    }`}
                  >
                    <Icon className="w-3.5 h-3.5 shrink-0" />
                    <span className="truncate">{cat.label}</span>
                  </button>
                );
              })}
            </div>
          </div>

          {/* User Info Fields (Read-Only / Authenticated User Identity) */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <div className="flex items-center justify-between mb-1">
                <label className="block text-xs font-medium text-slate-700">
                  Adınız Soyadınız
                </label>
                <span className="text-[10px] text-slate-400 font-medium flex items-center gap-1">
                  <Lock className="w-2.5 h-2.5 text-slate-400" />
                  Kilitli
                </span>
              </div>
              <input
                type="text"
                value={name}
                readOnly
                disabled
                placeholder="Giriş yapan kullanıcı adı"
                title="Hesabınıza ait isim otomatik atanmıştır ve değiştirilemez."
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-slate-100/80 text-slate-700 font-medium cursor-not-allowed select-none focus:outline-none"
              />
            </div>
            <div>
              <div className="flex items-center justify-between mb-1">
                <label className="block text-xs font-medium text-slate-700">
                  E-posta Adresiniz
                </label>
                <span className="text-[10px] text-slate-400 font-medium flex items-center gap-1">
                  <Lock className="w-2.5 h-2.5 text-slate-400" />
                  Kilitli
                </span>
              </div>
              <input
                type="email"
                value={email}
                readOnly
                disabled
                placeholder="ornek@domain.com"
                title="Hesabınıza ait e-posta adresi otomatik atanmıştır ve değiştirilemez."
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-slate-100/80 text-slate-700 font-medium cursor-not-allowed select-none focus:outline-none"
              />
            </div>
          </div>

          {/* Message Textarea */}
          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-xs font-medium text-slate-700">
                Açıklama &amp; Mesajınız <span className="text-rose-500">*</span>
              </label>
              <span className="text-[10px] text-slate-400">
                {message.length} karakter
              </span>
            </div>
            <textarea
              rows={4}
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              placeholder="Karşılaştığınız hata, adımlar, ekran görüntüsü açıklaması veya DevPilot AI için geliştirmesini istediğiniz yeni özellik..."
              className="w-full px-3 py-2.5 text-xs rounded-xl border border-slate-200 bg-slate-50/50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all text-slate-800 resize-none"
              required
            />
          </div>

          {/* File Attachment */}
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1">
              Ek Dosya (Ekran Görüntüsü, PDF veya Log)
            </label>

            <input
              type="file"
              ref={fileInputRef}
              onChange={handleFileChange}
              accept="image/*,.pdf,.txt,.log,.json"
              className="hidden"
            />

            {!file ? (
              <button
                type="button"
                onClick={() => fileInputRef.current?.click()}
                className="w-full border-2 border-dashed border-slate-200 hover:border-blue-400 rounded-xl p-3 text-center transition-all bg-slate-50/50 hover:bg-blue-50/30 flex items-center justify-center gap-2 text-xs text-slate-600 cursor-pointer group"
              >
                <Paperclip className="w-4 h-4 text-slate-400 group-hover:text-blue-500 transition-colors" />
                <span>
                  Dosya seçin veya buraya tıklayın{' '}
                  <span className="text-slate-400 text-[11px]">(Maks. 15 MB)</span>
                </span>
              </button>
            ) : (
              <div className="flex items-center justify-between p-2.5 rounded-xl border border-blue-200 bg-blue-50/50">
                <div className="flex items-center gap-2.5 min-w-0">
                  {filePreview ? (
                    <img
                      src={filePreview}
                      alt="Önizleme"
                      className="w-10 h-10 rounded-lg object-cover border border-blue-200 shrink-0"
                    />
                  ) : (
                    <div className="w-10 h-10 rounded-lg bg-blue-100 text-blue-600 flex items-center justify-center shrink-0">
                      {file.type.includes('pdf') ? (
                        <FileText className="w-5 h-5" />
                      ) : (
                        <ImageIcon className="w-5 h-5" />
                      )}
                    </div>
                  )}
                  <div className="min-w-0">
                    <div className="text-xs font-semibold text-slate-800 truncate">
                      {file.name}
                    </div>
                    <div className="text-[10px] text-slate-500">
                      {(file.size / (1024 * 1024)).toFixed(2)} MB • {file.type || 'Bilinmeyen tür'}
                    </div>
                  </div>
                </div>
                <button
                  type="button"
                  onClick={removeFile}
                  className="p-1 rounded-lg text-slate-400 hover:text-rose-600 hover:bg-rose-50 transition-colors"
                  title="Dosyayı kaldır"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>
            )}
          </div>

          {/* Error Message */}
          {errorMessage && (
            <div className="flex items-center gap-2 p-2.5 rounded-xl bg-rose-50 border border-rose-200 text-xs text-rose-700">
              <AlertCircle className="w-4 h-4 shrink-0 text-rose-600" />
              <span>{errorMessage}</span>
            </div>
          )}

          {/* Actions */}
          <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleResetAndClose}
              disabled={isSubmitting}
            >
              İptal
            </Button>
            <Button
              type="submit"
              variant="primary"
              size="sm"
              isLoading={isSubmitting}
              loadingText="İletiliyor..."
              className="gap-1.5"
            >
              <Send className="w-3.5 h-3.5" />
              <span>Gönder</span>
            </Button>
          </div>
        </form>
      )}
    </Modal>
  );
};
