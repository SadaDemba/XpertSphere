FROM node:24-alpine3.21 AS base
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
COPY src/frontend/packages/recruiter-app ./packages/recruiter-app

# Set working directory to the app
WORKDIR /app/packages/recruiter-app

# Build arguments for environment variables
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

# Build the app
RUN npm run build

# Expose port
EXPOSE 3001

# Start the app (Quasar serve the built files)
CMD ["npm", "run", "preview:recruiter"]