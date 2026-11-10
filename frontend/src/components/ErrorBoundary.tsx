import { Component, type ReactNode } from 'react';

type State = { hasError: boolean };

export default class ErrorBoundary extends Component<{ children: ReactNode }, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  componentDidCatch(error: unknown) {
    console.error(error);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="main">
          <div className="error">
            পাতাটি দেখাতে সমস্যা হয়েছে।{' '}
            <button onClick={() => window.location.reload()}>আবার চেষ্টা করুন</button>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}
