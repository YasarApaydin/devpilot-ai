import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate } from 'react-router-dom';
import { forgotPasswordSchema, type ForgotPasswordFormData } from '../utils/validation';
import { useAuth } from '../context/AuthContext';
import { Input } from '../components/ui/Input';
import { Button } from '../components/ui/Button';
import { Mail, CheckCircle2, ArrowLeft, RefreshCw, KeyRound, ArrowRight } from 'lucide-react';

export const ForgotPasswordPage: React.FC = () => {
  const { forgotPassword } = useAuth();
  const navigate = useNavigate();
  const [isSubmitted, setIsSubmitted] = useState<boolean>(false);
  const [submittedEmail, setSubmittedEmail] = useState<string>('');
  const [cooldown, setCooldown] = useState<number>(0);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    getValues,
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  });

  useEffect(() => {
    document.title = 'Şifremi Unuttum | DevPilot AI';
  }, []);

  useEffect(() => {
    let timer: any = null;
    if (cooldown > 0) {
      timer = setInterval(() => {
        setCooldown((prev) => prev - 1);
      }, 1000);
    }
    return () => {
      if (timer) clearInterval(timer);
    };
  }, [cooldown]);

  const onSubmit = async (data: ForgotPasswordFormData) => {
    try {
      await forgotPassword(data.email);
    } catch {
      // Güvenlik gereği e-posta sistemde var mı yok mu ifşa edilmez
    } finally {
      setSubmittedEmail(data.email);
      setIsSubmitted(true);
      setCooldown(60); // 60 saniye bekleme süresi
    }
  };

  const handleResend = async () => {
    if (cooldown > 0 || !submittedEmail) return;
    try {
      await forgotPassword(submittedEmail);
    } catch {
    } finally {
      setCooldown(60);
    }
  };

  return (
    <div className="bg-white border border-slate-200 rounded-2xl p-5 sm:p-8 shadow-sm">
      {isSubmitted ? (
        <div className="text-center py-2 space-y-4">
          <div className="w-14 h-14 rounded-2xl bg-blue-50 border border-blue-200 text-blue-600 flex items-center justify-center mx-auto shadow-2xs">
            <CheckCircle2 className="w-7 h-7" />
          </div>

          <div className="space-y-1.5">
            <h2 className="text-xl font-bold text-slate-900 tracking-tight">
              E-postanızı Kontrol Edin
            </h2>
            <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
              Eğer bu adres sistemimizde kayıtlı ise şifre sıfırlama bağlantısı gönderilmiştir:
            </p>
            <div className="inline-block mt-1 px-3 py-1 rounded-full bg-slate-100 text-slate-800 font-medium text-xs border border-slate-200">
              {submittedEmail}
            </div>
          </div>

          <div className="p-3.5 rounded-xl bg-slate-50 border border-slate-200 text-xs text-slate-600 text-left space-y-1.5">
            <div className="font-semibold text-slate-800">E-posta kutunuzda göremediniz mi?</div>
            <ul className="list-disc list-inside space-y-1 text-[11px] text-slate-500">
              <li>Gereksiz (Spam/Junk) klasörünü kontrol edin.</li>
              <li>Bağlantının geçerlilik süresi 2 saattir.</li>
            </ul>
          </div>

          {/* Aksiyon butonları */}
          <div className="space-y-2 pt-2">
            <Button
              type="button"
              variant="outline"
              size="md"
              className="w-full text-xs"
              onClick={handleResend}
              disabled={cooldown > 0}
            >
              <RefreshCw className={`w-3.5 h-3.5 mr-1.5 ${cooldown > 0 ? 'animate-spin' : ''}`} />
              {cooldown > 0 ? `Tekrar Gönder (${cooldown}s)` : 'E-postayı Tekrar Gönder'}
            </Button>

            <button
              type="button"
              onClick={() => {
                setIsSubmitted(false);
                setCooldown(0);
              }}
              className="text-xs text-slate-500 hover:text-slate-800 font-medium underline transition-colors cursor-pointer block mx-auto pt-1"
            >
              Farklı bir e-posta adresi dene
            </button>
          </div>

          <div className="pt-4 border-t border-slate-100 flex items-center justify-between text-xs">
            <Link
              to="/login"
              className="inline-flex items-center gap-1.5 font-semibold text-slate-600 hover:text-slate-900 transition-colors"
            >
              <ArrowLeft className="w-3.5 h-3.5" />
              <span>Giriş Yap</span>
            </Link>

            <Link
              to="/reset-password"
              className="inline-flex items-center gap-1 font-semibold text-blue-600 hover:text-blue-700 transition-colors"
            >
              <span>Kodum Var</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          </div>
        </div>
      ) : (
        <div>
          <div className="mb-6">
            <div className="w-10 h-10 rounded-xl bg-blue-50 border border-blue-200 text-blue-600 flex items-center justify-center mb-3 shadow-2xs">
              <KeyRound className="w-5 h-5" />
            </div>
            <h1 className="text-2xl font-bold text-slate-900 tracking-tight">Şifremi Unuttum</h1>
            <p className="text-xs text-slate-500 mt-1">
              DevPilot AI hesabınıza kayıtlı e-posta adresinizi girin, size güvenli bir şifre sıfırlama bağlantısı gönderelim.
            </p>
          </div>

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

            <Button
              type="submit"
              variant="primary"
              size="lg"
              className="w-full mt-2"
              isLoading={isSubmitting}
              loadingText="Bağlantı gönderiliyor..."
            >
              Sıfırlama Bağlantısı Gönder
            </Button>
          </form>

          <div className="mt-6 pt-5 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500">
            <Link
              to="/login"
              className="inline-flex items-center gap-1.5 hover:text-slate-800 transition-colors font-medium"
            >
              <ArrowLeft className="w-3.5 h-3.5" />
              <span>Giriş Ekranına Dön</span>
            </Link>

            <Link
              to="/reset-password"
              className="text-blue-600 hover:text-blue-700 font-semibold"
            >
              Sıfırlama kodum var
            </Link>
          </div>
        </div>
      )}
    </div>
  );
};
