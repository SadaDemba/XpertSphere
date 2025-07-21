FROM node:24-alpine3.21 AS base

RUN npm install -g @quasar/cli

# Set working directory
WORKDIR /app

# Copy workspace package files
COPY src/frontend/package*.json ./
COPY src/frontend/.prettierrc ./
COPY src/frontend/.prettierignore ./
COPY src/frontend/eslint.config.mjs ./

# Install workspace dependencies
RUN npm ci

# Copy specific app
COPY src/frontend/packages/candidate-app ./packages/candidate-app

# Build arguments for environment variables
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build:candidate

# Expose port
EXPOSE 3000

# Start the app (Quasar serve the built files)
CMD ["npm", "run", "preview:candidate"]
