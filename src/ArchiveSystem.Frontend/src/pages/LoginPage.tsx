import { Navigate } from 'react-router-dom';
import { Login } from '@/features/auth/components/Login';
import { useSessionStore } from '@/stores/useSessionStore';
import '@/features/auth/components/Login.css';

const LOGO_SRC = '/Logo/Logo.png';

export const LoginPage = () => {
  const isAuthenticated = useSessionStore((state) => state.isAuthenticated);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="auth-page-shell" dir="rtl">
      <div className="auth-page-frame">
        <header className="auth-page-brand" aria-label="معرفی سامانه">
          <div className="auth-brand-mark" aria-label="لوگوی سازمان">
            <img
              src={LOGO_SRC}
              alt="لوگوی سامانه بایگانی الکترونیک"
              onError={(event) => {
                event.currentTarget.hidden = true;
              }}
            />
            <span className="auth-brand-fallback">سامانه بایگانی الکترونیک</span>
          </div>
          <h1 className="auth-page-title">
            <span>سامانه بایگانی الکترونیک</span>
            <span>مدیریت آرشیو و پرونده‌ها</span>
          </h1>
          <p className="auth-kicker">درگاه امن دسترسی به سوابق و پرونده‌های سازمانی</p>
        </header>

        <section className="auth-page-card" aria-label="فرم ورود">
          <Login />
        </section>
      </div>
    </div>
  );
};
