import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useSearchParams, Link, useNavigate } from 'react-router-dom';
import { resetPasswordSchema, type ResetPasswordFormData } from '../utils/validation';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { Input } from '../components/ui/Input';
import { Button } from '../components/ui/Button';
import { PasswordStrengthIndicator } from '../components/ui/PasswordStrengthIndicator';
import { Lock, CheckCircle2, AlertCircle, Eye, EyeOff, Check, X, KeyRound, ArrowRight } from 'lucide-react';

export const ResetPasswordPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const urlToken = searchParams.get('token') || '';
  const navigate = useNavigate();

  const { resetPassword } = useAuth();
  const { showToast } = useToast();

  const [tokenValue, setTokenValue] = useState<string>(urlToken);
  const [isSuccess, setIsSuccess] = useState<boolean>(false);
  const [redirectCountdown, setRedirectCountdown] = useState<number>(4);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [showNewPassword, setShowNewPassword] = useState<boolean>(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState<boolean>(false);

  useEffect(() => {
    document.title = 'Yeni Şifre Belirle | DevPilot AI';
  }, []);

  useEffect(() => {
    if (urlToken) {
      setTokenValue(urlToken);
    }
  }, [urlToken]);

  // Auto redirect countdown upon success
  useEffect(() => {
    let interval: any = null;
    if (isSuccess && redirectCountdown > 0) {
      interval = setInterval(() => {
        setRedirectCountdown((prev) => prev - 1);
      }, 1000);
    } else if (isSuccess && redirectCountdown === 0) {
      navigate('/login', { replace: true });
    }
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [isSuccess, redirectCountdown, navigate]);

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      newPassword: '',
      confirmPassword: '',
    },
  });

  const watchedNewPassword = watch('newPassword') || '';
  const watchedConfirmPassword = watch('confirmPassword') || '';

  const isConfirmStarted = watchedConfirmPassword.length > 0;
  const isMatching = isConfirmStarted && watchedNewPassword.length > 0 && watchedNewPassword === watchedConfirmPassword;

  const onSubmit = async (data: ResetPasswordFormData) => {
    const activeToken = (tokenValue || urlToken).trim();
    if (!activeToken) {
      setErrorMessage('Lütfen e-postanıza gönderilen sıfırlama kodunu veya belirtecini giriniz.');
      return;
    }

    setErrorMessage(null);
    try {
      await resetPassword({
        token: activeToken,
        newPassword: data.newPassword,
        confirmPassword: data.confirmPassword,
      });
      setIsSuccess(true);
      showToast('Şifreniz başarıyla güncellendi.', 'success');
    } catch (err: any) {
      setErrorMessage(
        err?.message || 'Şifre güncellenemedi. Sıfırlama bağlantısının süresi dolmuş veya kod hatalı olabilir.'
      );
    }
  };

  return (
    <div className="bg-white border border-slate-200 rounded-2xl p-5 sm:p-8 shadow-sm">
      {isSuccess ? (
        <div className="text-center py-4 space-y-4">
          <div className="w-14 h-14 rounded-2xl bg-emerald-50 border border-emerald-200 text-emerald-600 flex items-center justify-center mx-auto shadow-2xs">
            <CheckCircle2 className="w-7 h-7" />
          </div>
          <div className="space-y-1">
            <h2 className="text-xl font-bold text-slate-900">Şifreniz Başarıyla Güncellendi!</h2>
            <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
              Hesabınızın şifresi yenilendi. Güvenliğiniz için eski tüm açık oturumlar sonlandırıldı.
            </p>
          </div>

          <div className="py-2">
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-slate-100 text-slate-700">
              Giriş sayfasına yönlendiriliyorsunuz ({redirectCountdown}s)...
            </span>
          </div>

          <Button
            variant="primary"
            size="lg"
            className="w-full mt-2"
            onClick={() => navigate('/login', { replace: true })}
          >
            <span>Hemen Giriş Yap</span>
            <ArrowRight className="w-4 h-4 ml-1.5" />
          </Button>
        </div>
      ) : (
        <div>
          <div className="mb-6">
            <div className="w-10 h-10 rounded-xl bg-blue-50 border border-blue-200 text-blue-600 flex items-center justify-center mb-3 shadow-2xs">
              <KeyRound className="w-5 h-5" />
            </div>
            <h1 className="text-2xl font-bold text-slate-900 tracking-tight">Yeni Şifre Belirle</h1>
            <p className="text-xs text-slate-500 mt-1">
              Güvenliğiniz için lütfen kriterlere uygun güçlü bir yeni şifre oluşturun.
            </p>
          </div>

          {errorMessage && (
            <div className="mb-5 p-3 rounded-lg bg-rose-50 border border-rose-200 text-rose-700 text-xs flex items-center gap-2.5">
              <AlertCircle className="w-4 h-4 shrink-0 text-rose-600" />
              <span>{errorMessage}</span>
            </div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            {/* If token was not in URL, let user paste it manually */}
            {!urlToken && (
              <div>
                <Input
                  label="Sıfırlama Kodu / Belirteci (Token)"
                  type="text"
                  placeholder="E-postanıza gönderilen sıfırlama kodunu yapıştırın"
                  value={tokenValue}
                  onChange={(e) => setTokenValue(e.target.value)}
                  leftIcon={<KeyRound className="w-4 h-4" />}
                />
                <p className="text-[11px] text-slate-400 mt-1">
                  E-postanızdaki bağlantıdan doğrudan geldiyseniz bu alan otomatik dolar.
                </p>
              </div>
            )}

            <div>
              <Input
                label="Yeni Şifre"
                type={showNewPassword ? 'text' : 'password'}
                placeholder="En az 8 karakter, harf, rakam ve sembol"
                autoComplete="new-password"
                leftIcon={<Lock className="w-4 h-4" />}
                rightIcon={
                  <button
                    type="button"
                    tabIndex={-1}
                    onClick={() => setShowNewPassword((prev) => !prev)}
                    className="text-slate-400 hover:text-slate-600 focus:outline-none transition-colors cursor-pointer"
                    title={showNewPassword ? 'Şifreyi gizle' : 'Şifreyi göster'}
                  >
                    {showNewPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                }
                error={errors.newPassword?.message}
                {...register('newPassword')}
              />
              <PasswordStrengthIndicator password={watchedNewPassword} />
            </div>

            <div>
              <Input
                label="Yeni Şifre Tekrarı"
                type={showConfirmPassword ? 'text' : 'password'}
                placeholder="Yeni şifrenizi tekrar giriniz"
                autoComplete="new-password"
                leftIcon={<Lock className="w-4 h-4" />}
                rightIcon={
                  <button
                    type="button"
                    tabIndex={-1}
                    onClick={() => setShowConfirmPassword((prev) => !prev)}
                    className="text-slate-400 hover:text-slate-600 focus:outline-none transition-colors cursor-pointer"
                    title={showConfirmPassword ? 'Şifreyi gizle' : 'Şifreyi göster'}
                  >
                    {showConfirmPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                }
                error={errors.confirmPassword?.message}
                {...register('confirmPassword')}
              />

              {isConfirmStarted && (
                <div
                  className={`mt-1.5 flex items-center gap-1.5 text-xs font-medium px-2 py-1 rounded-md border ${
                    isMatching
                      ? 'bg-emerald-50 border-emerald-200 text-emerald-700'
                      : 'bg-rose-50 border-rose-200 text-rose-700'
                  }`}
                >
                  {isMatching ? (
                    <>
                      <Check className="w-3.5 h-3.5 text-emerald-600 shrink-0" />
                      <span>Şifreler eşleşiyor</span>
                    </>
                  ) : (
                    <>
                      <X className="w-3.5 h-3.5 text-rose-600 shrink-0" />
                      <span>Şifreler henüz eşleşmiyor</span>
                    </>
                  )}
                </div>
              )}
            </div>

            <Button
              type="submit"
              variant="primary"
              size="lg"
              className="w-full mt-2"
              isLoading={isSubmitting}
              loadingText="Şifre güncelleniyor..."
            >
              Şifreyi Güncelle ve Kaydet
            </Button>
          </form>

          <div className="mt-6 pt-5 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500">
            <Link to="/forgot-password" className="hover:text-slate-800 transition-colors">
              Yeni kod iste
            </Link>
            <Link to="/login" className="text-blue-600 hover:text-blue-700 font-semibold">
              Giriş Ekranına Dön
            </Link>
          </div>
        </div>
      )}
    </div>
  );
};
