export interface AiModelInfo {
  id: string;
  name: string;
  provider: string;
  description: string;
  isDefault?: boolean;
  contextWindow?: number;
}

export interface UserAiKey {
  provider: string;
  displayName: string;
  isConfigured: boolean;
  keyHint?: string;
  isValid: boolean;
  lastTestedAt?: string;
  preferredModel?: string;
  docUrl: string;
  availableModels: AiModelInfo[];
}

export interface SaveAiKeyRequest {
  provider: string;
  apiKey: string;
  preferredModel?: string;
}

export interface TestAiKeyRequest {
  provider: string;
  apiKey?: string;
}

export interface TestAiKeyResponse {
  success: boolean;
  message: string;
  models?: AiModelInfo[];
}

export interface AiModelsResponse {
  defaultProvider: string;
  defaultModel: string;
  models: AiModelInfo[];
}

export interface SetAiPreferenceRequest {
  provider: string;
  model: string;
}
