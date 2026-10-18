import React from 'react';
import { Link } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Bot, ArrowLeft } from 'lucide-react';

export const NotFoundPage: React.FC = () => {
  return (
    <div className="min-h-screen bg-[#f8fafc] flex flex-col items-center justify-center p-6 text-center text-slate-800">
      <img
        src="/app-logo.png"
        alt="DevPilot AI"
        className="w-16 h-16 rounded-full object-cover mb-6 shadow-sm border border-slate-200"
      />

      <h1 className="text-4xl font-extrabold text-slate-900 tracking-tight mb-2">404</h1>
      <h2 className="text-lg font-semibold text-slate-700 mb-2">Page Not Found</h2>
      <p className="text-xs text-slate-500 max-w-sm mb-6 leading-relaxed">
        The page you are looking for doesn't exist or has been moved to another URL.
      </p>

      <Link to="/">
        <Button variant="primary" size="md" leftIcon={<ArrowLeft className="w-4 h-4" />}>
          Return Home
        </Button>
      </Link>
    </div>
  );
};
