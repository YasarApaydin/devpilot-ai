import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate } from 'react-router-dom';
import { registerSchema, type RegisterFormData } from '../utils/validation';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { Input } from '../components/ui/Input';
import { Button } from '../components/ui/Button';
import { PasswordStrengthIndicator } from '../components/ui/PasswordStrengthIndicator';
import { User, Mail, Lock, AlertCircle, Eye, EyeOff, Check, X } from 'lucide-react';
import type { ApiError } from '../types';

export const RegisterPage: React.FC = () => {
  const navigate = useNavigate();
  const { register: registerUser } = useAuth();
  const { showToast } = useToast();

  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState<boolean>(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState<boolean>(false);

  useEffect(() => {
    document.title = 'Hesap Oluştur | DevPilot AI';
  }, []);

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      password: '',
      confirmPassword: '',
    },
  });

  const watchedPassword = watch('password') || '';
  const watchedConfirmPassword = watch('confirmPassword') || '';

  const isConfirmStarted = watchedConfirmPassword.length > 0;
  const isMatching = isConfirmStarted && watchedPassword.length > 0 && watchedPassword === watchedConfirmPassword;

  const onSubmit = async (data: RegisterFormData) => {
    setErrorMessage(null);

    try {
      await registerUser(data);
      showToast('Doğrulama kodu e-postanıza gönderildi.', 'success');
      // Kayıt işleminden sonra kullanıcı doğrudan içeri alınmaz; kod doğrulama ekranına yönlendirilir
      navigate(`/verify-email?email=${encodeURIComponent(data.email)}`);
    } catch (err: any) {
      const apiErr = err as ApiError;
      if (
        apiErr.statusCode === 409 ||
        apiErr.message?.includes('zaten') ||
        apiErr.message?.includes('already exists')
      ) {
        setErrorMessage('Bu e-posta adresi ile kayıtlı ve onaylanmış bir hesap zaten bulunmaktadır.');
      } else if (apiErr.statusCode === 429) {
        setErrorMessage('Çok fazla istek yapıldı. Lütfen daha sonra tekrar deneyiniz.');
      } else {
        setErrorMessage(apiErr.message || 'Kayıt başarısız oldu. Lütfen bilgilerinizi kontrol ediniz.');
      }
    }
  };

  return (
    <div className="bg-white border border-slate-200 rounded-2xl p-5 sm:p-8 shadow-sm">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900 tracking-tight">Hesap Oluştur</h1>
        <p className="text-xs text-slate-500 mt-1">
          Kişisel AI asistanınız ve döküman çalışma alanınızı kullanmaya başlayın.
        </p>
      </div>

      {errorMessage && (
        <div className="mb-5 p-3 rounded-lg bg-rose-50 border border-rose-200 text-rose-700 text-xs flex items-center gap-2.5">
          <AlertCircle className="w-4 h-4 shrink-0 text-rose-600" />
          <span>{errorMessage}</span>
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3.5">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <Input
            label="Ad"
            placeholder="Alex"
            leftIcon={<User className="w-4 h-4" />}
            error={errors.firstName?.message}
            {...register('firstName')}
          />

          <Input
            label="Soyad"
            placeholder="Carter"
            leftIcon={<User className="w-4 h-4" />}
            error={errors.lastName?.message}
            {...register('lastName')}
          />
        </div>

        <Input
          label="E-posta Adresi"
          type="email"
          placeholder="ad@sirket.com"
          autoComplete="email"
          leftIcon={<Mail className="w-4 h-4" />}
          error={errors.email?.message}
          {...register('email')}
        />

        <div>
          <Input
            label="Şifre"
            type={showPassword ? 'text' : 'password'}
            placeholder="En az 8 karakter, harf, rakam, sembol"
            autoComplete="new-password"
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
          {/* Live password criteria checklist */}
          <PasswordStrengthIndicator password={watchedPassword} />
        </div>

        <div>
          <Input
            label="Şifre Tekrarı"
            type={showConfirmPassword ? 'text' : 'password'}
            placeholder="Şifrenizi onaylayın"
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

          {/* Canlı Şifre Eşleşme Bildirimi */}
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
                  <span>Şifreler uyuşuyor</span>
                </>
              ) : (
                <>
                  <X className="w-3.5 h-3.5 text-rose-600 shrink-0" />
                  <span>Şifreler uyuşmuyor</span>
                </>
              )}
            </div>
          )}
        </div>

        <Button
          type="submit"
          variant="primary"
          size="lg"
          className="w-full mt-3"
          isLoading={isSubmitting}
          loadingText="Hesap oluşturuluyor..."
        >
          Hesap Oluştur
        </Button>
      </form>

      <div className="mt-6 pt-5 border-t border-slate-100 text-center text-xs text-slate-500">
        Zaten hesabınız var mı?{' '}
        <Link to="/login" className="text-blue-600 hover:text-blue-700 font-semibold">
          Giriş Yap
        </Link>
      </div>
    </div>
  );
};
