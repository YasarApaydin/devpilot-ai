import React, { useState, useCallback, useMemo } from 'react';
import { Navigate, Outlet, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useConversations } from '../hooks/useConversations';
import { useLibrary } from '../hooks/useLibrary';
import { TopBar } from '../components/layout/TopBar';
import { MobileFooter } from '../components/layout/MobileFooter';
import { ConversationSidebar } from '../components/conversation/ConversationSidebar';
import { LibraryPanel } from '../components/library/LibraryPanel';
import { Spinner } from '../components/ui/Spinner';
import { X } from 'lucide-react';

export const AppLayout: React.FC = () => {
  const { isAuthenticated, isLoading } = useAuth();
  const navigate = useNavigate();
  const params = useParams<{ conversationId?: string }>();

  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [libraryOpen, setLibraryOpen] = useState(false);
  const [isCreatingNew, setIsCreatingNew] = useState(false);

  const isAuthReady = isAuthenticated && !isLoading;

  const {
    conversations,
    isLoading: isLoadingConvs,
    createConversation,
    renameConversation,
    deleteConversation,
    togglePin,
  } = useConversations(params.conversationId, isAuthReady);

  const {
    documents,
    searchTerm,
    setSearchTerm,
    isLoading: isLoadingDocs,
    isUploading,
    uploadProgress,
    uploadError,
    uploadDocument,
    deleteDocument,
  } = useLibrary(isAuthReady);

  const handleSelectConversation = useCallback((id: string) => {
    navigate(`/app/chat/${id}`);
    setSidebarOpen(false);
  }, [navigate]);

  const handleNewConversation = useCallback(async (state?: any) => {
    setIsCreatingNew(true);
    try {
      const newConv = await createConversation();
      const isEvent =
        state &&
        typeof state === 'object' &&
        ('nativeEvent' in state || '_reactName' in state || 'stopPropagation' in state);
      const navigationState = isEvent ? undefined : state;
      navigate(`/app/chat/${newConv.id}`, navigationState ? { state: navigationState } : undefined);
      setSidebarOpen(false);
      return newConv;
    } finally {
      setIsCreatingNew(false);
    }
  }, [createConversation, navigate]);

  const handleSidebarNewConversation = useCallback(() => {
    handleNewConversation();
  }, [handleNewConversation]);

  const handleDeleteConversation = useCallback(async (id: string) => {
    await deleteConversation(id);
    if (params.conversationId === id) {
      const remaining = conversations.filter((c) => c.id !== id);
      if (remaining.length > 0) {
        navigate(`/app/chat/${remaining[0].id}`);
      } else {
        navigate('/app');
      }
    }
  }, [deleteConversation, params.conversationId, conversations, navigate]);

  const outletContext = useMemo(
    () => ({
      conversations,
      activeConversationId: params.conversationId,
      handleNewConversation,
    }),
    [conversations, params.conversationId, handleNewConversation]
  );

  // Redirect if not logged in
  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#f8fafc] flex items-center justify-center">
        <Spinner size="lg" className="text-blue-600" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="h-screen w-screen flex flex-col bg-[#f8fafc] text-slate-800 overflow-hidden select-none">
      {/* Top Bar Header */}
      <TopBar
        onToggleSidebar={() => setSidebarOpen((prev) => !prev)}
        onToggleLibrary={() => setLibraryOpen((prev) => !prev)}
        isSidebarOpen={sidebarOpen}
        isLibraryOpen={libraryOpen}
      />

      {/* Main 3-Column Body */}
      <div className="flex-1 flex overflow-hidden relative">
        {/* Left Column: Conversation Sidebar (Desktop) */}
        <div className="hidden lg:block w-72 h-full shrink-0">
          <ConversationSidebar
            conversations={conversations}
            activeConversationId={params.conversationId}
            isLoading={isLoadingConvs}
            onSelect={handleSelectConversation}
            onNewConversation={handleSidebarNewConversation}
            onRename={renameConversation}
            onDelete={handleDeleteConversation}
            onTogglePin={togglePin}
            isCreatingNew={isCreatingNew}
          />
        </div>

        {/* Mobile Sidebar Drawer */}
        {sidebarOpen && (
          <div className="fixed inset-0 z-50 lg:hidden flex">
            <div
              className="fixed inset-0 bg-slate-900/50 backdrop-blur-xs animate-fade-in transition-opacity"
              onClick={() => setSidebarOpen(false)}
            />
            <div className="relative w-72 sm:w-80 max-w-[85vw] h-full bg-[#f8fafc] z-10 shadow-2xl flex flex-col animate-drawer-left">
              <div className="p-3 border-b border-slate-200 flex justify-between items-center bg-white">
                <span className="text-xs font-semibold text-slate-800">Sohbetler</span>
                <button
                  onClick={() => setSidebarOpen(false)}
                  className="p-1.5 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-slate-100 cursor-pointer transition-colors"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>
              <div className="flex-1 overflow-hidden">
                <ConversationSidebar
                  conversations={conversations}
                  activeConversationId={params.conversationId}
                  isLoading={isLoadingConvs}
                  onSelect={handleSelectConversation}
                  onNewConversation={handleSidebarNewConversation}
                  onRename={renameConversation}
                  onDelete={handleDeleteConversation}
                  onTogglePin={togglePin}
                  isCreatingNew={isCreatingNew}
                />
              </div>
            </div>
          </div>
        )}

        {/* Middle Column: Chat Window or Page Content */}
        <main className="flex-1 h-full flex flex-col overflow-hidden bg-[#f8fafc]">
          <Outlet context={outletContext} />
        </main>

        {/* Right Column: Library Panel (Desktop XL) */}
        <div className="hidden xl:block w-80 h-full shrink-0">
          <LibraryPanel
            documents={documents}
            searchTerm={searchTerm}
            onSearchChange={setSearchTerm}
            isLoading={isLoadingDocs}
            isUploading={isUploading}
            uploadProgress={uploadProgress}
            uploadError={uploadError}
            onUpload={uploadDocument}
            onDelete={deleteDocument}
          />
        </div>

        {/* Mobile / Tablet Library Drawer */}
        {libraryOpen && (
          <div className="fixed inset-0 z-50 xl:hidden flex justify-end">
            <div
              className="fixed inset-0 bg-slate-900/50 backdrop-blur-xs animate-fade-in transition-opacity"
              onClick={() => setLibraryOpen(false)}
            />
            <div className="relative w-80 max-w-[85vw] h-full bg-[#f8fafc] z-10 shadow-2xl flex flex-col animate-drawer-right">
              <div className="p-3 border-b border-slate-200 flex justify-between items-center bg-white">
                <span className="text-xs font-semibold text-slate-800">Doküman Kütüphanesi</span>
                <button
                  onClick={() => setLibraryOpen(false)}
                  className="p-1.5 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-slate-100 cursor-pointer transition-colors"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>
              <div className="flex-1 overflow-hidden">
                <LibraryPanel
                  documents={documents}
                  searchTerm={searchTerm}
                  onSearchChange={setSearchTerm}
                  isLoading={isLoadingDocs}
                  isUploading={isUploading}
                  uploadProgress={uploadProgress}
                  uploadError={uploadError}
                  onUpload={uploadDocument}
                  onDelete={deleteDocument}
                />
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Modern Responsive Mobile Footer / Bottom Navigation */}
      <MobileFooter
        onToggleSidebar={() => setSidebarOpen((prev) => !prev)}
        onToggleLibrary={() => setLibraryOpen((prev) => !prev)}
        onNewConversation={handleSidebarNewConversation}
      />
    </div>
  );
};
