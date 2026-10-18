import type { User } from '../types/auth';
import type { Conversation } from '../types/conversation';
import type { ChatMessage } from '../types/chat';
import type { LibraryDocument } from '../types/library';

// In-memory mock storage
export const MOCK_USER: User = {
  id: 'usr_01jk98m20000000000000001',
  email: 'developer@example.com',
  firstName: 'Alex',
  lastName: 'Carter',
  emailVerified: true,
  createdAt: new Date(Date.now() - 86400000 * 30).toISOString(),
};

export const MOCK_CONVERSATIONS: Conversation[] = [
  {
    id: 'conv_1',
    title: 'LangGraph Memory Architecture',
    createdAt: new Date(Date.now() - 3600000 * 3).toISOString(),
    updatedAt: new Date(Date.now() - 3600000 * 1).toISOString(),
    messageCount: 4,
  },
  {
    id: 'conv_2',
    title: '.NET Clean Architecture Integration',
    createdAt: new Date(Date.now() - 86400000 * 1).toISOString(),
    updatedAt: new Date(Date.now() - 86400000 * 1).toISOString(),
    messageCount: 6,
  },
  {
    id: 'conv_3',
    title: 'Azure Deployment & Docker Setup',
    createdAt: new Date(Date.now() - 86400000 * 3).toISOString(),
    updatedAt: new Date(Date.now() - 86400000 * 3).toISOString(),
    messageCount: 2,
  },
];

export const MOCK_MESSAGES: Record<string, ChatMessage[]> = {
  conv_1: [
    {
      id: 'msg_1',
      conversationId: 'conv_1',
      role: 'user',
      content: 'LangGraph içinde InMemorySaver kullanarak conversation state nasıl tutulur?',
      createdAt: new Date(Date.now() - 3600000 * 2).toISOString(),
      inputType: 'text',
    },
    {
      id: 'msg_2',
      conversationId: 'conv_1',
      role: 'assistant',
      content:
        'LangGraph içerisinde `InMemorySaver` kullanarak conversation state tutmak için agent oluşturulurken `checkpointer` parametresine saver örneğini iletiriz.\n\n```python\nfrom langgraph.checkpoint.memory import InMemorySaver\nfrom langchain.agents import create_agent\n\nmemory = InMemorySaver()\nagent = create_agent(\n    model=llm,\n    tools=[web_search],\n    checkpointer=memory\n)\n```\n\nBu sayede her `thread_id` (yani `conversationId`) için geçmiş context korunur ve sonraki turlarda önceki mesajlar hatırlanır.',
      createdAt: new Date(Date.now() - 3600000 * 2 + 5000).toISOString(),
      inputType: 'text',
      hasPdf: true,
    },
    {
      id: 'msg_3',
      conversationId: 'conv_1',
      role: 'user',
      content: 'Sesli komutla çalışırken de bu memory geçerli mi?',
      createdAt: new Date(Date.now() - 3600000 * 1).toISOString(),
      inputType: 'voice',
    },
    {
      id: 'msg_4',
      conversationId: 'conv_1',
      role: 'assistant',
      content:
        'Evet! Sesli kayıt Whisper tarafından metne dönüştürüldükten sonra normal `HumanMessage` olarak agent zincirine iletilir. Böylece aynı `conversationId` altında hem sesli hem yazılı girdiler tek bir hafızada birleştirilir.',
      createdAt: new Date(Date.now() - 3600000 * 1 + 4000).toISOString(),
      inputType: 'text',
      hasPdf: true,
    },
  ],
  conv_2: [
    {
      id: 'msg_201',
      conversationId: 'conv_2',
      role: 'user',
      content: '.NET 9 Clean Architecture projesinde AI Orchestration katmanını nereye koymalıyım?',
      createdAt: new Date(Date.now() - 86400000 * 1).toISOString(),
      inputType: 'text',
    },
    {
      id: 'msg_202',
      conversationId: 'conv_2',
      role: 'assistant',
      content:
        'Clean Architecture prensiplerine göre:\n\n1. **Core / Application Katmanı**: `IAiService` ve `IChatAgent` arayüzlerini ve DTO modellerini tanımlayın.\n2. **Infrastructure Katmanı**: Python AI agent veya Groq/LangChain servisi ile iletişim kuran `HttpAiServiceClient` implementasyonunu buraya yerleştirin.\n3. **API Katmanı**: Controller ve Minimal API endpointleri sadece Application MediatR handler veya service katmanını çağırır.',
      createdAt: new Date(Date.now() - 86400000 * 1 + 6000).toISOString(),
      inputType: 'text',
      hasPdf: true,
    },
  ],
};

export const MOCK_DOCUMENTS: LibraryDocument[] = [
  {
    id: 'doc_1',
    fileName: 'CleanArchitecture.pdf',
    size: 2450000,
    status: 'Processed',
    fileType: 'application/pdf',
    createdAt: new Date(Date.now() - 86400000 * 5).toISOString(),
    updatedAt: new Date(Date.now() - 86400000 * 5).toISOString(),
    summary: 'Clean architecture design patterns and domain driven design guide.',
  },
  {
    id: 'doc_2',
    fileName: 'LangChainNotes.pdf',
    size: 1180000,
    status: 'Processing',
    fileType: 'application/pdf',
    createdAt: new Date(Date.now() - 86400000 * 2).toISOString(),
    updatedAt: new Date(Date.now() - 86400000 * 2).toISOString(),
    summary: 'Memory management, vector embeddings, and tool routing patterns.',
  },
  {
    id: 'doc_3',
    fileName: 'Resume.pdf',
    size: 420000,
    status: 'Ready',
    fileType: 'application/pdf',
    createdAt: new Date(Date.now() - 86400000 * 1).toISOString(),
    updatedAt: new Date(Date.now() - 86400000 * 1).toISOString(),
    summary: 'Full-stack software engineering resume and skills portfolio.',
  },
];
