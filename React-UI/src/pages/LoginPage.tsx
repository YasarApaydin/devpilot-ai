import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { loginSchema, type LoginFormData } from '../utils/validation';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { Input } from '../components/ui/Input';
import { Button } from '../components/ui/Button';
import { Lock, Mail, AlertCircle, Eye, EyeOff, ShieldAlert, ArrowRight, CheckCircle2 } from 'lucide-react';
import { cookieConsentService } from '../services/cookieConsentService';
import type { ApiError } from '../types';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const wasDeleted = searchParams.get('deleted') === '1';
  const { login } = useAuth();
  const { showToast } = useToast();

  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [unverifiedEmail, setUnverifiedEmail] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState<boolean>(false);

  useEffect(() => {
    document.title = 'Giriş Yap | DevPilot AI';
  }, []);

  const savedEmail = typeof window !== 'undefined' ? localStorage.getItem('devpilot_remembered_email') || '' : '';
  const isRemembered = typeof window !== 'undefined' ? localStorage.getItem('devpilot_remember_me') === 'true' : false;

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: savedEmail,
      password: '',
      rememberMe: isRemembered,
    },
  });

  const onSubmit = async (data: LoginFormData) => {
    setErrorMessage(null);
    setUnverifiedEmail(null);

    try {
      if (typeof window !== 'undefined') {
        if (data.rememberMe && cookieConsentService.hasConsent('functional')) {
          localStorage.setItem('devpilot_remembered_email', data.email);
          localStorage.setItem('devpilot_remember_me', 'true');
        } else {
          localStorage.removeItem('devpilot_remembered_email');
          localStorage.removeItem('devpilot_remember_me');
        }
      }

      await login(data);
      showToast('Giriş başarılı! Hoş geldiniz.', 'success');
      navigate('/app', { replace: true });
    } catch (err: any) {
      const apiErr = err as ApiError;
      if (
        apiErr.statusCode === 403 ||
        apiErr.code === 'EMAIL_NOT_VERIFIED' ||
        apiErr.message?.includes('doğrulanmamış') ||
        apiErr.message?.includes('verified')
      ) {
        setUnverifiedEmail(data.email);
        setErrorMessage('E-posta adresiniz henüz doğrulanmamıştır. Lütfen hesabınızı onaylayınız.');
      } else if (apiErr.statusCode === 429) {
        setErrorMessage('Çok fazla hatalı deneme yapıldı. Lütfen daha sonra tekrar deneyiniz.');
      } else {
        // Şifre veya mail hatalı olduğunda hangisi olduğunu açık etmeden standart güvenlik mesajı
        setErrorMessage('E-posta adresiniz veya şifreniz hatalıdır.');
      }
    }
  };

  return (
    <div className="bg-white border border-slate-200 rounded-2xl p-5 sm:p-8 shadow-sm">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900 tracking-tight">Giriş Yap</h1>
        <p className="text-xs text-slate-500 mt-1">
          Yapay zeka asistanınız ve döküman çalışma alanınıza erişin.
        </p>
      </div>

      {wasDeleted && (
        <div className="mb-5 p-3.5 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-800 text-xs space-y-1 shadow-2xs">
          <div className="flex items-center gap-2 font-semibold text-emerald-950">
            <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
            <span>Hesabınız Kalıcı Olarak Silindi</span>
          </div>
          <p className="text-emerald-800/90 leading-relaxed text-[11.5px]">
            DevPilot AI hesabınız, sohbet geçmişiniz, kayıtlı dokümanlarınız ve tüm API anahtarlarınız kalıcı olarak silindi. Sistemdeki tüm oturumlarınız kapatıldı.
          </p>
        </div>
      )}

      {errorMessage && (
        <div className="mb-5 p-3 rounded-lg bg-rose-50 border border-rose-200 text-rose-700 text-xs space-y-2">
          <div className="flex items-center gap-2.5">
            {unverifiedEmail ? (
              <ShieldAlert className="w-4 h-4 shrink-0 text-amber-600" />
            ) : (
              <AlertCircle className="w-4 h-4 shrink-0 text-rose-600" />
            )}
            <span className="font-medium">{errorMessage}</span>
          </div>

          {/* E-posta doğrulanmamışsa doğrudan kod ekranına yönlendirme butonu */}
          {unverifiedEmail && (
            <div className="pt-1 border-t border-rose-200/60 flex justify-end">
              <button
                type="button"
                onClick={() => navigate(`/verify-email?email=${encodeURIComponent(unverifiedEmail)}`)}
                className="text-xs font-semibold text-blue-700 hover:text-blue-800 flex items-center gap-1 cursor-pointer"
              >
                <span>Doğrulama Kodunu Gir</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </button>
            </div>
          )}
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <Input
          label="E-posta Adresi"
          type="email"
          placeholder="ad@sirket.com"
          autoComplete="email"
          leftIcon={<Mail className="w-4 h-4" />}
          error={errors.email?.message}
          {...register('email')}
        />

        <Input
          label="Şifre"
          type={showPassword ? 'text' : 'password'}
          placeholder="••••••••"
          autoComplete="current-password"
          leftIcon={<Lock className="w-4 h-4" />}
          rightIcon={
            <button
              type="button"
              tabIndex={-1}
              onClick={() => setShowPassword((prev) => !prev)}
              className="text-slate-400 hover:text-slate-600 focus:outline-none transition-colors cursor-pointer"
              title={showPassword ? 'Şifreyi gizle' : 'Şifreyi göster'}
            >
              {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
            </button>
          }
          error={errors.password?.message}
          {...register('password')}
        />

        <div className="flex items-center justify-between text-xs pt-1">
          <label className="flex items-center gap-2 text-slate-600 cursor-pointer select-none">
            <input
              type="checkbox"
              className="rounded border-slate-300 text-blue-600 focus:ring-blue-500 w-3.5 h-3.5"
              {...register('rememberMe')}
            />
            Beni hatırla
          </label>
          <Link
            to="/forgot-password"
            className="text-blue-600 hover:text-blue-700 transition-colors font-medium"
          >
            Şifremi unuttum?
          </Link>
        </div>

        <Button
          type="submit"
          variant="primary"
          size="lg"
          className="w-full mt-2"
          isLoading={isSubmitting}
          loadingText="Giriş yapılıyor..."
        >
          Giriş Yap
        </Button>
      </form>

      <div className="mt-6 pt-5 border-t border-slate-100 text-center text-xs text-slate-500">
        Hesabınız yok mu?{' '}
        <Link to="/register" className="text-blue-600 hover:text-blue-700 font-semibold">
          Hesap oluştur
        </Link>
      </div>
    </div>
  );
};
