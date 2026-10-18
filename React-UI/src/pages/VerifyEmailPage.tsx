import React, { useState, useEffect, useRef } from 'react';
import { useSearchParams, Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { Button } from '../components/ui/Button';
import { CheckCircle2, AlertCircle, MailCheck, ArrowRight, RefreshCw } from 'lucide-react';
import { Spinner } from '../components/ui/Spinner';

export const VerifyEmailPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const tokenFromUrl = searchParams.get('token');
  const emailFromUrl = searchParams.get('email') || '';

  const { verifyEmail, resendVerification } = useAuth();
  const { showToast } = useToast();
  const navigate = useNavigate();

  // State
  const [digits, setDigits] = useState<string[]>(['', '', '', '', '', '']);
  const [isVerifying, setIsVerifying] = useState<boolean>(false);
  const [isSuccess, setIsSuccess] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [resendCooldown, setResendCooldown] = useState<number>(0);
  const [isResending, setIsResending] = useState<boolean>(false);

  useEffect(() => {
    document.title = 'E-posta Doğrulama | DevPilot AI';
  }, []);

  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

  // Automatic token verification if token present in URL
  useEffect(() => {
    if (tokenFromUrl) {
      const verifyToken = async () => {
        setIsVerifying(true);
        setErrorMessage(null);
        try {
          await verifyEmail({ token: tokenFromUrl });
          setIsSuccess(true);
          showToast('E-posta adresiniz başarıyla doğrulandı!', 'success');
        } catch (err: any) {
          if (err?.message?.includes('expired') || err?.statusCode === 400) {
            setErrorMessage('Doğrulama bağlantısının süresi dolmuş.');
          } else {
            setErrorMessage(err?.message || 'Doğrulama bağlantısı geçersiz veya süresi dolmuş.');
          }
        } finally {
          setIsVerifying(false);
        }
      };

      verifyToken();
    }
  }, [tokenFromUrl, verifyEmail, showToast]);

  // Resend countdown timer
  useEffect(() => {
    if (resendCooldown <= 0) return;
    const interval = setInterval(() => {
      setResendCooldown((prev) => prev - 1);
    }, 1000);
    return () => clearInterval(interval);
  }, [resendCooldown]);

  // Digit box change handler
  const handleDigitChange = (index: number, value: string) => {
    if (!/^\d*$/.test(value)) return;

    const newDigits = [...digits];
    newDigits[index] = value.slice(-1);
    setDigits(newDigits);

    // Auto-advance
    if (value && index < 5) {
      inputRefs.current[index + 1]?.focus();
    }
  };

  const handleKeyDown = (index: number, e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Backspace' && !digits[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
  };

  const handlePaste = (e: React.ClipboardEvent<HTMLInputElement>) => {
    e.preventDefault();
    const pasted = e.clipboardData.getData('text').trim();
    if (/^\d{6}$/.test(pasted)) {
      const parts = pasted.split('');
      setDigits(parts);
      inputRefs.current[5]?.focus();
    }
  };

  const handleVerifyCode = async (e: React.FormEvent) => {
    e.preventDefault();
    const code = digits.join('');
    if (code.length !== 6) {
      setErrorMessage('Lütfen 6 haneli doğrulama kodunun tamamını giriniz.');
      return;
    }

    setIsVerifying(true);
    setErrorMessage(null);
    try {
      await verifyEmail({ code });
      setIsSuccess(true);
      showToast('E-posta adresiniz başarıyla doğrulandı.', 'success');
    } catch (err: any) {
      setErrorMessage(err?.message || 'Geçersiz veya süresi dolmuş doğrulama kodu.');
    } finally {
      setIsVerifying(false);
    }
  };

  const handleResend = async () => {
    if (resendCooldown > 0 || isResending) return;
    if (!emailFromUrl) {
      setErrorMessage('E-posta adresi belirtilmediği için kod tekrar gönderilemedi. Lütfen kayıt ekranından tekrar deneyiniz.');
      return;
    }

    setIsResending(true);
    setErrorMessage(null);
    try {
      await resendVerification(emailFromUrl);
      showToast('Yeni doğrulama kodu gönderildi!', 'success');
      setResendCooldown(45);
    } catch (err: any) {
      showToast(err?.message || 'Kod gönderilemedi.', 'error');
    } finally {
      setIsResending(false);
    }
  };

  return (
    <div className="bg-white border border-slate-200 rounded-2xl p-5 sm:p-8 shadow-sm text-center">
      {/* Success State */}
      {isSuccess ? (
        <div className="py-4 space-y-4">
          <div className="w-14 h-14 rounded-2xl bg-emerald-50 border border-emerald-200 text-emerald-600 flex items-center justify-center mx-auto">
            <CheckCircle2 className="w-7 h-7" />
          </div>
          <h2 className="text-xl font-bold text-slate-900">E-posta Doğrulandı!</h2>
          <p className="text-xs text-slate-500 max-w-xs mx-auto">
            E-posta adresiniz başarıyla onaylandı. Şimdi kullanıcı bilgilerinizle sisteme giriş yapabilirsiniz.
          </p>
          <Button
            variant="primary"
            size="lg"
            className="w-full mt-4"
            rightIcon={<ArrowRight className="w-4 h-4" />}
            onClick={() => navigate('/login')}
          >
            Giriş Yap
          </Button>
        </div>
      ) : tokenFromUrl && isVerifying ? (
        /* Token Verifying State */
        <div className="py-8 space-y-4">
          <Spinner size="lg" className="text-blue-600 mx-auto" />
          <h3 className="text-base font-semibold text-slate-900">E-posta doğrulanıyor...</h3>
          <p className="text-xs text-slate-500">Güvenlik bağlantınız onaylanırken lütfen bekleyiniz.</p>
        </div>
      ) : (
        /* Code Input State or Token Error */
        <div>
          <div className="w-12 h-12 rounded-2xl bg-blue-50 border border-blue-200 text-blue-600 flex items-center justify-center mx-auto mb-4">
            <MailCheck className="w-6 h-6" />
          </div>

          <h1 className="text-2xl font-bold text-slate-900 tracking-tight">E-postanızı Doğrulayın</h1>
          <p className="text-xs text-slate-500 mt-1 mb-6">
            {emailFromUrl ? (
              <>
                <span className="font-semibold text-slate-700">{emailFromUrl}</span> adresine gönderilen 6 haneli güvenlik kodunu giriniz.
              </>
            ) : (
              'E-posta adresinize gönderilen 6 haneli güvenlik kodunu giriniz.'
            )}
          </p>

          {errorMessage && (
            <div className="mb-5 p-3 rounded-lg bg-rose-50 border border-rose-200 text-rose-700 text-xs flex items-center gap-2 text-left">
              <AlertCircle className="w-4 h-4 shrink-0 text-rose-600" />
              <span>{errorMessage}</span>
            </div>
          )}

          <form onSubmit={handleVerifyCode} className="space-y-6">
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-3">
                6 Haneli Doğrulama Kodu
              </label>
              {/* 6-digit inputs [ _ _ _ _ _ _ ] */}
              <div className="flex justify-center gap-1.5 sm:gap-2.5" onPaste={handlePaste}>
                {digits.map((digit, i) => (
                  <input
                    key={i}
                    ref={(el) => {
                      inputRefs.current[i] = el;
                    }}
                    type="text"
                    inputMode="numeric"
                    maxLength={1}
                    value={digit}
                    onChange={(e) => handleDigitChange(i, e.target.value)}
                    onKeyDown={(e) => handleKeyDown(i, e)}
                    className="w-9.5 h-11 sm:w-11 sm:h-12 text-center text-base sm:text-lg font-bold rounded-lg bg-slate-50 border border-slate-300 text-slate-900 focus:bg-white focus:border-blue-500 focus:ring-2 focus:ring-blue-500/10 focus:outline-none transition-all shadow-2xs"
                  />
                ))}
              </div>
            </div>

            <Button
              type="submit"
              variant="primary"
              size="lg"
              className="w-full"
              isLoading={isVerifying}
              loadingText="Doğrulanıyor..."
            >
              Hesabı Doğrula
            </Button>
          </form>

          {/* Resend button with countdown */}
          <div className="mt-6 pt-5 border-t border-slate-100 flex flex-col items-center gap-3">
            <button
              type="button"
              disabled={resendCooldown > 0 || isResending}
              onClick={handleResend}
              className="text-xs font-medium text-slate-600 hover:text-blue-600 disabled:text-slate-400 disabled:cursor-not-allowed flex items-center gap-1.5 transition-colors cursor-pointer"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${isResending ? 'animate-spin' : ''}`} />
              {resendCooldown > 0
                ? `Tekrar göndermek için ${resendCooldown}s bekleyin`
                : isResending
                ? 'Kod gönderiliyor...'
                : 'Doğrulama Kodunu Tekrar Gönder'}
            </button>

            <Link to="/login" className="text-xs text-blue-600 hover:text-blue-700 font-semibold">
              Giriş Ekranına Dön
            </Link>
          </div>
        </div>
      )}
    </div>
  );
};
