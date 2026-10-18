import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { userApi } from '../api/userApi';
import {
  profileSchema,
  changePasswordSchema,
  type ProfileFormData,
  type ChangePasswordFormData,
} from '../utils/validation';
import { Input } from '../components/ui/Input';
import { Button } from '../components/ui/Button';
import { Badge } from '../components/ui/Badge';
import { PasswordStrengthIndicator } from '../components/ui/PasswordStrengthIndicator';
import { Modal } from '../components/ui/Modal';
import { User, Mail, ShieldCheck, Lock, LogOut, Trash2, AlertTriangle, Cpu, Eye, EyeOff } from 'lucide-react';
import { AiKeysSection } from '../components/profile/AiKeysSection';
import { PrivacyPreferencesSection } from '../components/profile/PrivacyPreferencesSection';

type ProfileTab = 'profile' | 'ai-keys' | 'privacy';

export const ProfilePage: React.FC = () => {
  const { user, updateUser, logout } = useAuth();
  const { showToast } = useToast();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const tabParam = searchParams.get('tab');
  const initialTab: ProfileTab =
    tabParam === 'ai-keys' ? 'ai-keys' : tabParam === 'privacy' ? 'privacy' : 'profile';
  const [activeTab, setActiveTab] = useState<ProfileTab>(initialTab);

  useEffect(() => {
    if (activeTab === 'ai-keys') {
      document.title = 'AI Modellerim | DevPilot AI';
    } else if (activeTab === 'privacy') {
      document.title = 'Gizlilik ve Çerezler | DevPilot AI';
    } else {
      document.title = 'Profilim | DevPilot AI';
    }
  }, [activeTab]);

  useEffect(() => {
    const currentTabParam = searchParams.get('tab');
    if (currentTabParam === 'ai-keys' && activeTab !== 'ai-keys') {
      setActiveTab('ai-keys');
    } else if (currentTabParam === 'privacy' && activeTab !== 'privacy') {
      setActiveTab('privacy');
    }
  }, [searchParams]);

  const handleTabChange = (tab: ProfileTab) => {
    setActiveTab(tab);
    setSearchParams(tab === 'profile' ? {} : { tab });
  };

  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isDeletingAccount, setIsDeletingAccount] = useState(false);

  // Profile form
  const {
    register: registerProfile,
    handleSubmit: handleProfileSubmit,
    formState: { errors: profileErrors, isSubmitting: isProfileSubmitting },
  } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      firstName: user?.firstName || '',
      lastName: user?.lastName || '',
    },
  });

  // Change password form
  const {
    register: registerPassword,
    handleSubmit: handlePasswordSubmit,
    reset: resetPasswordForm,
    watch: watchPassword,
    formState: { errors: passwordErrors, isSubmitting: isPasswordSubmitting },
  } = useForm<ChangePasswordFormData>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: {
      currentPassword: '',
      newPassword: '',
      confirmNewPassword: '',
    },
  });

  const watchedNewPassword = watchPassword('newPassword');

  const onUpdateProfile = async (data: ProfileFormData) => {
    try {
      const res = await userApi.updateProfile(data);
      if (res.data) {
        updateUser(res.data);
        showToast('Profile updated successfully.', 'success');
      }
    } catch (err: any) {
      showToast(err?.message || 'Failed to update profile.', 'error');
    }
  };

  const onChangePassword = async (data: ChangePasswordFormData) => {
    try {
      await userApi.changePassword({
        currentPassword: data.currentPassword,
        newPassword: data.newPassword,
      });
      showToast('Şifreniz başarıyla değiştirildi.', 'success');
      resetPasswordForm();
      setShowCurrentPassword(false);
      setShowNewPassword(false);
      setShowConfirmPassword(false);
      setIsChangingPassword(false);
    } catch (err: any) {
      showToast(err?.message || 'Şifre değiştirilirken bir hata oluştu.', 'error');
    }
  };

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const handleDeleteAccount = async () => {
    try {
      setIsDeletingAccount(true);
      await userApi.deleteAccount();
      // Purge all tokens, caches, and storage completely
      if (typeof window !== 'undefined') {
        try {
          localStorage.clear();
          sessionStorage.clear();
        } catch {}
        // Clean full redirect to login with confirmation
        window.location.href = '/login?deleted=1';
      }
    } catch (err: any) {
      showToast(err?.message || 'Hesap silinirken bir hata oluştu.', 'error');
      setIsDeletingAccount(false);
      setIsDeleteModalOpen(false);
    }
  };

  return (
    <div className="flex-1 h-full overflow-y-auto p-4 sm:p-6 bg-[#f8fafc] animate-fade-in">
      <div className="max-w-2xl mx-auto space-y-5">
        <div>
          <h1 className="text-xl font-bold text-slate-900 tracking-tight">
            {activeTab === 'ai-keys'
              ? 'AI Modellerim & API Anahtarlarım'
              : activeTab === 'privacy'
              ? 'Gizlilik ve Çerez Tercihleri'
              : 'Hesap Profili'}
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            {activeTab === 'ai-keys'
              ? 'Kendi OpenAI, Gemini, Groq, xAI ve Claude API anahtarlarınızı yönetin.'
              : activeTab === 'privacy'
              ? 'Çerez onaylarınızı, yerel depolama tercihlerinizi ve KVKK haklarınızı yönetin.'
              : 'Hesap bilgilerinizi, parolanızı ve güvenlik ayarlarınızı yönetin.'}
          </p>
        </div>

        {/* Tab Navigation - Scrollable on mobile */}
        <div className="flex items-center gap-2 border-b border-slate-200 pb-2 overflow-x-auto whitespace-nowrap">
          <button
            type="button"
            onClick={() => handleTabChange('profile')}
            className={`px-3.5 sm:px-4 py-2 rounded-xl text-xs font-semibold transition-all shrink-0 cursor-pointer ${
              activeTab === 'profile'
                ? 'bg-blue-600 text-white shadow-xs'
                : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
            }`}
          >
            Hesap Bilgileri &amp; Güvenlik
          </button>
          <button
            type="button"
            onClick={() => handleTabChange('ai-keys')}
            className={`px-3.5 sm:px-4 py-2 rounded-xl text-xs font-semibold flex items-center gap-1.5 transition-all shrink-0 cursor-pointer ${
              activeTab === 'ai-keys'
                ? 'bg-blue-600 text-white shadow-xs'
                : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
            }`}
          >
            <Cpu className="w-3.5 h-3.5" />
            <span>AI Modellerim / API Anahtarlarım</span>
          </button>
          <button
            type="button"
            onClick={() => handleTabChange('privacy')}
            className={`px-3.5 sm:px-4 py-2 rounded-xl text-xs font-semibold flex items-center gap-1.5 transition-all shrink-0 cursor-pointer ${
              activeTab === 'privacy'
                ? 'bg-blue-600 text-white shadow-xs'
                : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
            }`}
          >
            <ShieldCheck className="w-3.5 h-3.5" />
            <span>Gizlilik &amp; Çerezler</span>
          </button>
        </div>

        {activeTab === 'ai-keys' ? (
          <AiKeysSection />
        ) : activeTab === 'privacy' ? (
          <PrivacyPreferencesSection />
        ) : (
          <>
            {/* Profile Card */}
            <div className="p-4 sm:p-6 rounded-2xl bg-white border border-slate-200 shadow-sm animate-scale-in">
              <form onSubmit={handleProfileSubmit(onUpdateProfile)} className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Input
                label="First Name"
                leftIcon={<User className="w-4 h-4" />}
                error={profileErrors.firstName?.message}
                {...registerProfile('firstName')}
              />

              <Input
                label="Last Name"
                leftIcon={<User className="w-4 h-4" />}
                error={profileErrors.lastName?.message}
                {...registerProfile('lastName')}
              />
            </div>

            <div>
              <label className="block text-xs font-medium text-slate-700 mb-1.5">
                Email Address
              </label>
              <div className="flex items-center gap-2">
                <div className="flex-1 relative">
                  <Input
                    type="email"
                    value={user?.email || ''}
                    disabled
                    leftIcon={<Mail className="w-4 h-4" />}
                    className="opacity-70 cursor-not-allowed bg-slate-50 text-slate-600"
                  />
                </div>
                <Badge
                  status={user?.emailVerified ? 'verified' : 'unverified'}
                  className="shrink-0 h-9 px-3 flex items-center gap-1"
                >
                  <ShieldCheck className="w-3.5 h-3.5" />
                  {user?.emailVerified ? 'Verified' : 'Unverified'}
                </Badge>
              </div>
            </div>

            <div className="pt-2 flex justify-end">
              <Button
                type="submit"
                variant="primary"
                size="sm"
                isLoading={isProfileSubmitting}
                loadingText="Saving..."
              >
                Save Changes
              </Button>
            </div>
          </form>
        </div>

        {/* Password Security Card */}
        <div className="p-6 rounded-2xl bg-white border border-slate-200 shadow-sm">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h3 className="text-sm font-semibold text-slate-900">Password & Security</h3>
              <p className="text-xs text-slate-500 mt-0.5">
                Ensure your account uses a secure password.
              </p>
            </div>
            {!isChangingPassword && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => setIsChangingPassword(true)}
              >
                Change Password
              </Button>
            )}
          </div>

          {isChangingPassword && (
            <form onSubmit={handlePasswordSubmit(onChangePassword)} className="space-y-4 pt-2">
              <Input
                label="Mevcut Şifre"
                type={showCurrentPassword ? 'text' : 'password'}
                placeholder="Mevcut şifrenizi girin"
                autoComplete="current-password"
                leftIcon={<Lock className="w-4 h-4" />}
                rightIcon={
                  <button
                    type="button"
                    tabIndex={-1}
                    onClick={() => setShowCurrentPassword((prev) => !prev)}
                    className="text-slate-400 hover:text-slate-600 focus:outline-none transition-colors cursor-pointer"
                    title={showCurrentPassword ? 'Şifreyi gizle' : 'Şifreyi göster'}
                  >
                    {showCurrentPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                }
                error={passwordErrors.currentPassword?.message}
                {...registerPassword('currentPassword')}
              />

              <div>
                <Input
                  label="Yeni Şifre"
                  type={showNewPassword ? 'text' : 'password'}
                  placeholder="En az 8 karakter, harf, rakam, sembol"
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
                  error={passwordErrors.newPassword?.message}
                  {...registerPassword('newPassword')}
                />
                <PasswordStrengthIndicator password={watchedNewPassword} />
              </div>

              <Input
                label="Yeni Şifre Tekrarı"
                type={showConfirmPassword ? 'text' : 'password'}
                placeholder="Yeni şifrenizi onaylayın"
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
                error={passwordErrors.confirmNewPassword?.message}
                {...registerPassword('confirmNewPassword')}
              />

              <div className="flex items-center justify-end gap-2 pt-2">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    setIsChangingPassword(false);
                    setShowCurrentPassword(false);
                    setShowNewPassword(false);
                    setShowConfirmPassword(false);
                  }}
                >
                  Vazgeç
                </Button>
                <Button
                  type="submit"
                  variant="primary"
                  size="sm"
                  isLoading={isPasswordSubmitting}
                  loadingText="Güncelleniyor..."
                >
                  Şifreyi Güncelle
                </Button>
              </div>
            </form>
          )}
        </div>

        {/* Sign Out Card */}
        <div className="p-5 rounded-2xl bg-white border border-slate-200 shadow-2xs flex items-center justify-between">
          <div>
            <h4 className="text-xs font-semibold text-slate-800">Session Management</h4>
            <p className="text-xs text-slate-500 mt-0.5">
              Sign out from this device to end your active session.
            </p>
          </div>
          <Button
            variant="outline"
            size="sm"
            leftIcon={<LogOut className="w-3.5 h-3.5" />}
            onClick={handleLogout}
          >
            Sign Out
          </Button>
        </div>

        {/* Danger Zone - Delete Account */}
        <div className="p-5 rounded-2xl bg-rose-50/50 border border-rose-200 shadow-2xs">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <div className="flex items-center gap-1.5 text-rose-600 font-semibold text-xs">
                <AlertTriangle className="w-3.5 h-3.5" />
                <span>Tehlikeli Bölge</span>
              </div>
              <h4 className="text-sm font-semibold text-slate-900 mt-1">Hesabımı Sil</h4>
              <p className="text-xs text-slate-500 mt-0.5 max-w-md">
                Hesabınızı ve bu hesaba bağlı tüm sohbetleri, mesajları ve kütüphanenizdeki dokümanları kalıcı olarak siler. Bu işlem geri alınamaz.
              </p>
            </div>
            <Button
              variant="danger"
              size="sm"
              leftIcon={<Trash2 className="w-3.5 h-3.5" />}
              onClick={() => setIsDeleteModalOpen(true)}
              className="shrink-0"
            >
              Hesabı Sil
            </Button>
          </div>
        </div>
      </>
    )}

        {/* Delete Account Confirmation Modal */}
        <Modal
          isOpen={isDeleteModalOpen}
          onClose={() => !isDeletingAccount && setIsDeleteModalOpen(false)}
          title="Hesabınızı Silmek İstediğinize Emin Misiniz?"
          description="Bu işlem kalıcıdır ve geri alınamaz."
        >
          <div className="space-y-4 pt-1">
            <div className="p-3.5 rounded-xl bg-rose-50 border border-rose-200 text-xs text-rose-900 space-y-1.5">
              <p className="font-semibold flex items-center gap-1.5 text-rose-700">
                <AlertTriangle className="w-4 h-4 text-rose-600 shrink-0" />
                Dikkat: Tüm verileriniz kalıcı olarak silinecektir!
              </p>
              <p className="text-slate-600 leading-relaxed pl-5.5">
                DevPilot AI hesabınızı sildiğinizde; tüm sohbet geçmişiniz, gönderilen mesajlar, oturum jetonları ve kütüphanenize yüklediğiniz tüm PDF dokümanları veritabanından tamamen silinecektir.
              </p>
            </div>

            <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={isDeletingAccount}
                onClick={() => setIsDeleteModalOpen(false)}
              >
                Vazgeç
              </Button>
              <Button
                type="button"
                variant="danger"
                size="sm"
                isLoading={isDeletingAccount}
                loadingText="Hesap Siliniyor..."
                leftIcon={<Trash2 className="w-3.5 h-3.5" />}
                onClick={handleDeleteAccount}
              >
                Evet, Hesabımı Kalıcı Olarak Sil
              </Button>
            </div>
          </div>
        </Modal>
      </div>
    </div>
  );
};
