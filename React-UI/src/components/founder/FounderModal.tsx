import React from 'react';
import { Modal } from '../ui/Modal';
import {
  Mail,
  Phone,
  ExternalLink,
  Award,
  BookOpen,
  Briefcase,
  GraduationCap,
  Code2,
  CheckCircle2,
  MapPin,
  Sparkles,
  ShieldCheck,
  Cpu,
} from 'lucide-react';

const LinkedInIcon: React.FC<{ className?: string }> = ({ className = 'w-3.5 h-3.5' }) => (
  <svg className={className} fill="currentColor" viewBox="0 0 24 24">
    <path d="M19 3a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h14m-.5 15.5v-5.3a3.26 3.26 0 0 0-3.26-3.26c-.85 0-1.84.52-2.28 1.3v-1.11h-2.79v8.37h2.79v-4.93c0-.77.62-1.4 1.39-1.4a1.4 1.4 0 0 1 1.4 1.4v4.93h2.75M6.46 8.76a1.68 1.68 0 1 0 0-3.36 1.68 1.68 0 0 0 0 3.36m1.4 9.74v-8.37H5.06v8.37z" />
  </svg>
);

const GitHubIcon: React.FC<{ className?: string }> = ({ className = 'w-3.5 h-3.5' }) => (
  <svg className={className} fill="currentColor" viewBox="0 0 24 24">
    <path fillRule="evenodd" clipRule="evenodd" d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.53 1.032 1.53 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z" />
  </svg>
);

export interface FounderModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const FounderModal: React.FC<FounderModalProps> = ({ isOpen, onClose }) => {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title=""
      maxWidth="xl"
    >
      <div className="space-y-6 -mt-2">
        {/* Top Header Card */}
        <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-slate-900 via-blue-950 to-slate-900 p-6 text-white shadow-md border border-blue-900/50">
          <div className="absolute top-0 right-0 -mt-6 -mr-6 w-36 h-36 bg-blue-500/20 rounded-full blur-2xl pointer-events-none" />
          
          <div className="relative flex flex-col sm:flex-row items-center sm:items-start gap-5">
            {/* Profile Photo */}
            <div className="relative group shrink-0">
              <div className="w-24 h-24 sm:w-28 sm:h-28 rounded-2xl overflow-hidden ring-4 ring-blue-500/30 shadow-xl bg-slate-800 flex items-center justify-center">
                <img
                  src="/yasar-apaydin.jpg"
                  alt="Yaşar Apaydın"
                  className="w-full h-full object-cover object-top"
                  onError={(e) => {
                    // Fallback to avatar placeholder if image is missing
                    (e.target as HTMLElement).style.display = 'none';
                  }}
                />
              </div>
              <span className="absolute -bottom-1 -right-1 px-2 py-0.5 rounded-full text-[10px] font-bold tracking-wider uppercase bg-emerald-500 text-white shadow-xs border-2 border-slate-900">
                Lead
              </span>
            </div>

            {/* Profile Info */}
            <div className="flex-1 text-center sm:text-left space-y-2">
              <div className="flex flex-wrap items-center justify-center sm:justify-start gap-2">
                <h2 className="text-xl sm:text-2xl font-bold tracking-tight text-white">
                  Yaşar Apaydın
                </h2>
                <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
                  İş Fırsatlarına Açık
                </span>
              </div>

              <p className="text-sm font-medium text-blue-200 flex items-center justify-center sm:justify-start gap-1.5">
                <img src="/app-logo.png" alt="DevPilot AI" className="w-4 h-4 rounded-full object-cover shrink-0" />
                <span>DevPilot AI Geliştiricisi &amp; Sistem Mimarı (Lead Software Engineer &amp; Developer)</span>
              </p>

              <p className="text-xs text-slate-300 flex flex-wrap items-center justify-center sm:justify-start gap-x-3 gap-y-1">
                <span className="flex items-center gap-1">
                  <Cpu className="w-3.5 h-3.5 text-blue-400" />
                  Backend Developer (.NET / C#)
                </span>
                <span className="hidden sm:inline">•</span>
                <span className="flex items-center gap-1">
                  <Code2 className="w-3.5 h-3.5 text-indigo-400" />
                  Full Stack Developer
                </span>
                <span className="hidden sm:inline">•</span>
                <span className="flex items-center gap-1">
                  <Sparkles className="w-3.5 h-3.5 text-cyan-400" />
                  AI Agent &amp; LLM Engineer
                </span>
              </p>

              {/* Social / Contact Quick Action Badges */}
              <div className="pt-2 flex flex-wrap items-center justify-center sm:justify-start gap-2">
                <a
                  href="https://www.linkedin.com/in/ya%C5%9Far-apaydin-541647230/"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-blue-600/30 hover:bg-blue-600 text-blue-100 hover:text-white border border-blue-500/30 text-xs font-medium transition-all"
                >
                  <LinkedInIcon className="w-3.5 h-3.5" />
                  <span>LinkedIn</span>
                  <ExternalLink className="w-2.5 h-2.5 opacity-70" />
                </a>

                <a
                  href="https://github.com/YasarApaydin"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-100 border border-slate-700 text-xs font-medium transition-all"
                >
                  <GitHubIcon className="w-3.5 h-3.5" />
                  <span>GitHub</span>
                  <ExternalLink className="w-2.5 h-2.5 opacity-70" />
                </a>

                <a
                  href="https://orcid.org/0009-0003-9303-5586"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-emerald-900/30 hover:bg-emerald-800/50 text-emerald-200 border border-emerald-600/30 text-xs font-medium transition-all"
                >
                  <Award className="w-3.5 h-3.5 text-emerald-400" />
                  <span>ORCID: 0009-0003-9303-5586</span>
                  <ExternalLink className="w-2.5 h-2.5 opacity-70" />
                </a>

                <a
                  href="mailto:apaydinyasar0@gmail.com"
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-rose-900/30 hover:bg-rose-800/50 text-rose-200 border border-rose-600/30 text-xs font-medium transition-all"
                >
                  <Mail className="w-3.5 h-3.5" />
                  <span>apaydinyasar0@gmail.com</span>
                </a>

                <a
                  href="tel:+905415062981"
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 text-xs font-medium transition-all"
                >
                  <Phone className="w-3.5 h-3.5 text-emerald-400" />
                  <span>0541 506 29 81</span>
                </a>
              </div>
            </div>
          </div>
        </div>

        {/* Content Tabs / Body */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs text-slate-700">
          {/* Box 1: Proje & Mimari (DevPilot AI) */}
          <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 space-y-2.5">
            <div className="flex items-center gap-2 text-slate-900 font-semibold text-sm">
              <ShieldCheck className="w-4 h-4 text-blue-600" />
              <span>DevPilot AI Mimarisi &amp; Başarılar</span>
            </div>
            <p className="text-slate-600 leading-relaxed">
              DevPilot AI'ın tüm backend mimarisini, veritabanı şemasını, çok ajanlı yapay zekâ entegrasyonunu ve güvenlik tarama altyapısını sıfırdan kurmuştur.
            </p>
            <ul className="space-y-1.5 text-slate-600">
              <li className="flex items-start gap-1.5">
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600 shrink-0 mt-0.5" />
                <span><strong>Backend:</strong> .NET 9, ASP.NET Core Web API, Clean Architecture, CQRS &amp; MediatR.</span>
              </li>
              <li className="flex items-start gap-1.5">
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600 shrink-0 mt-0.5" />
                <span><strong>Veritabanı &amp; Önbellek:</strong> PostgreSQL, Entity Framework Core, Redis.</span>
              </li>
              <li className="flex items-start gap-1.5">
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600 shrink-0 mt-0.5" />
                <span><strong>AI &amp; LLM Entegrasyonu:</strong> Groq API, yerel Ollama (Qwen2.5-Coder), akıllı fallback ve streaming.</span>
              </li>
              <li className="flex items-start gap-1.5">
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600 shrink-0 mt-0.5" />
                <span><strong>Güvenlik Analiz Motoru:</strong> Native güvenlik açığı tarama mekanizmaları ve onaylı siber analiz süreçleri.</span>
              </li>
            </ul>
          </div>

          {/* Box 2: Akademik Başarılar & Yayınlar */}
          <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 space-y-2.5">
            <div className="flex items-center gap-2 text-slate-900 font-semibold text-sm">
              <Award className="w-4 h-4 text-amber-600" />
              <span>Akademik Çalışmalar &amp; Projeler</span>
            </div>
            
            <div className="p-2.5 rounded-lg bg-white border border-slate-200 space-y-1">
              <div className="flex items-center justify-between text-xs font-semibold text-blue-700">
                <span>TÜBİTAK 2209-A Yürütücüsü</span>
                <span className="text-[10px] text-slate-400 font-normal">Desteklendi</span>
              </div>
              <p className="text-[11px] text-slate-600">
                Üniversite Öğrencileri Araştırma Projeleri Destekleme Programı kapsamında yapay zekâ odaklı AR-GE projesi başarıyla yürütülmüştür.
              </p>
            </div>

            <div className="p-2.5 rounded-lg bg-white border border-slate-200 space-y-1">
              <div className="flex items-center justify-between text-xs font-semibold text-indigo-700">
                <span>Karadeniz Fen Bilimleri Dergisi (KFB)</span>
                <span className="text-[10px] text-slate-400 font-normal">Akademik Yayın</span>
              </div>
              <p className="text-[11px] text-slate-600">
                Hakemli bilimsel dergide yapay zekâ ve bilgisayar bilimleri üzerine makalesi yayınlanmıştır.
              </p>
            </div>

            <div className="flex items-center gap-1.5 pt-1 text-[11px] text-slate-500">
              <GraduationCap className="w-3.5 h-3.5 text-slate-400" />
              <span>Recep Tayyip Erdoğan Üniversitesi (Bilgisayar Müh.) &amp; Anadolu Üni. (YBS)</span>
            </div>
          </div>
        </div>

        {/* Technical Skills Badges */}
        <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 space-y-2">
          <div className="flex items-center gap-2 text-slate-900 font-semibold text-xs">
            <Code2 className="w-3.5 h-3.5 text-blue-600" />
            <span>Teknik Uzmanlıklar &amp; Yetenekler</span>
          </div>
          <div className="flex flex-wrap gap-1.5 text-[11px]">
            {[
              'C#',
              '.NET 9',
              'ASP.NET Core',
              'Clean Architecture',
              'CQRS / MediatR',
              'Entity Framework Core',
              'PostgreSQL',
              'Redis',
              'Docker',
              'React 19',
              'TypeScript',
              'Tailwind CSS',
              'Python',
              'AI Agents & Multi-Agent Architecture',
              'LangGraph',
              'Ollama & Local LLMs',
              'Groq API',
              'Prompt Engineering',
              'Application Security & Pentest',
              'Git / GitHub CI/CD',
            ].map((tech) => (
              <span
                key={tech}
                className="px-2.5 py-1 rounded-md bg-white border border-slate-200 text-slate-700 font-medium shadow-2xs hover:border-blue-300 hover:text-blue-600 transition-colors"
              >
                {tech}
              </span>
            ))}
          </div>
        </div>

        {/* Contact & Hire CTA */}
        <div className="p-4 rounded-xl bg-blue-50/70 border border-blue-200 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs">
          <div className="space-y-0.5 text-center sm:text-left">
            <div className="font-semibold text-blue-900 flex items-center justify-center sm:justify-start gap-1.5">
              <Briefcase className="w-4 h-4 text-blue-600" />
              <span>Ekibiniz için güçlü bir mühendis mi arıyorsunuz?</span>
            </div>
            <p className="text-blue-700 text-[11px]">
              Backend Developer, Full Stack Developer veya AI/LLM Engineer pozisyonları için iletişime geçebilirsiniz.
            </p>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            <a
              href="mailto:apaydinyasar0@gmail.com"
              className="px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-medium shadow-xs transition-colors"
            >
              E-posta Gönder
            </a>
            <a
              href="https://www.linkedin.com/in/ya%C5%9Far-apaydin-541647230/"
              target="_blank"
              rel="noopener noreferrer"
              className="px-3.5 py-1.5 rounded-lg bg-white border border-blue-300 text-blue-700 hover:bg-blue-50 font-medium transition-colors"
            >
              LinkedIn'de Bağlantı Kur
            </a>
          </div>
        </div>
      </div>
    </Modal>
  );
};
