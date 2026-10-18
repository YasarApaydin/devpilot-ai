import { z } from 'zod';

export interface PasswordCriterion {
  id: string;
  label: string;
  met: boolean;
}

export function evaluatePassword(password: string = '') {
  const criteria: PasswordCriterion[] = [
    {
      id: 'length',
      label: 'En az 8 karakter',
      met: password.length >= 8,
    },
    {
      id: 'lowercase',
      label: 'En az bir küçük harf (a-z)',
      met: /[a-z]/.test(password),
    },
    {
      id: 'uppercase',
      label: 'En az bir büyük harf (A-Z)',
      met: /[A-Z]/.test(password),
    },
    {
      id: 'number',
      label: 'En az bir rakam (0-9)',
      met: /[0-9]/.test(password),
    },
    {
      id: 'special',
      label: 'En az bir özel karakter (!@#$%^&* vb.)',
      met: /[^A-Za-z0-9]/.test(password),
    },
  ];

  const metCount = criteria.filter((c) => c.met).length;
  let strengthLabel = 'Çok Zayıf';
  let strengthColor = 'bg-rose-500';

  if (metCount >= 5) {
    strengthLabel = 'Güçlü';
    strengthColor = 'bg-emerald-500';
  } else if (metCount >= 3) {
    strengthLabel = 'Orta';
    strengthColor = 'bg-amber-500';
  } else if (metCount >= 2) {
    strengthLabel = 'Zayıf';
    strengthColor = 'bg-orange-500';
  }

  return {
    criteria,
    metCount,
    total: criteria.length,
    isValid: metCount === criteria.length,
    percent: (metCount / criteria.length) * 100,
    strengthLabel,
    strengthColor,
  };
}

// Reusable password schema
export const securePasswordSchema = z
  .string()
  .min(8, 'Şifre en az 8 karakter olmalıdır')
  .regex(/[a-z]/, 'Şifre en az bir küçük harf içermelidir')
  .regex(/[A-Z]/, 'Şifre en az bir büyük harf içermelidir')
  .regex(/[0-9]/, 'Şifre en az bir rakam içermelidir')
  .regex(/[^A-Za-z0-9]/, 'Şifre en az bir özel karakter içermelidir');

export const loginSchema = z.object({
  email: z.string().min(1, 'E-posta adresi gereklidir').email('Geçerli bir e-posta adresi giriniz'),
  password: z.string().min(1, 'Şifre gereklidir'),
  rememberMe: z.boolean().optional(),
});

export type LoginFormData = z.infer<typeof loginSchema>;

export const registerSchema = z
  .object({
    firstName: z.string().trim().min(1, 'Ad alanı zorunludur').max(50, 'En fazla 50 karakter'),
    lastName: z.string().trim().min(1, 'Soyad alanı zorunludur').max(50, 'En fazla 50 karakter'),
    email: z.string().min(1, 'E-posta adresi gereklidir').email('Geçerli bir e-posta adresi giriniz'),
    password: securePasswordSchema,
    confirmPassword: z.string().min(1, 'Lütfen şifrenizi tekrar giriniz'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Şifreler birbiriyle eşleşmiyor',
    path: ['confirmPassword'],
  });

export type RegisterFormData = z.infer<typeof registerSchema>;

export const forgotPasswordSchema = z.object({
  email: z.string().min(1, 'E-posta adresi gereklidir').email('Geçerli bir e-posta adresi giriniz'),
});

export type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;

export const resetPasswordSchema = z
  .object({
    newPassword: securePasswordSchema,
    confirmPassword: z.string().min(1, 'Lütfen yeni şifrenizi tekrar giriniz'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Şifreler birbiriyle eşleşmiyor',
    path: ['confirmPassword'],
  });

export type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

export const verifyCodeSchema = z.object({
  code: z
    .string()
    .length(6, 'Doğrulama kodu 6 haneli olmalıdır')
    .regex(/^\d{6}$/, 'Kod sadece rakamlardan oluşmalıdır'),
});

export type VerifyCodeFormData = z.infer<typeof verifyCodeSchema>;

export const profileSchema = z.object({
  firstName: z.string().trim().min(1, 'Ad alanı zorunludur'),
  lastName: z.string().trim().min(1, 'Soyad alanı zorunludur'),
});

export type ProfileFormData = z.infer<typeof profileSchema>;

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Mevcut şifre gereklidir'),
    newPassword: securePasswordSchema,
    confirmNewPassword: z.string().min(1, 'Lütfen yeni şifrenizi tekrar giriniz'),
  })
  .refine((data) => data.newPassword === data.confirmNewPassword, {
    message: 'Yeni şifreler birbiriyle eşleşmiyor',
    path: ['confirmNewPassword'],
  });

export type ChangePasswordFormData = z.infer<typeof changePasswordSchema>;
