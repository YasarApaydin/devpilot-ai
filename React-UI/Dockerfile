# syntax=docker/dockerfile:1
# Multi-stage Dockerfile for React-UI (Vite + React 19 + TailwindCSS)

# Stage 1: Build the React Application
FROM node:20-alpine AS build
WORKDIR /app

# Copy dependency manifests
COPY package.json package-lock.json* ./
RUN npm install

# Copy application source code
COPY . .

# Build production bundle
ARG VITE_API_BASE_URL=/api
ARG VITE_APP_NAME="DevPilot AI"
ARG VITE_MAX_UPLOAD_MB=20
ARG VITE_REQUEST_TIMEOUT_MS=30000
ARG VITE_ENABLE_MOCK_FALLBACK=false

ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_APP_NAME=$VITE_APP_NAME
ENV VITE_MAX_UPLOAD_MB=$VITE_MAX_UPLOAD_MB
ENV VITE_REQUEST_TIMEOUT_MS=$VITE_REQUEST_TIMEOUT_MS
ENV VITE_ENABLE_MOCK_FALLBACK=$VITE_ENABLE_MOCK_FALLBACK

RUN npm run build

# Stage 2: Serve with Nginx
FROM nginx:alpine AS final

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy compiled static assets from build stage
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
