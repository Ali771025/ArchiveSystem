FROM node:22-alpine AS build
WORKDIR /app

COPY src/ArchiveSystem.Frontend/package*.json ./
RUN npm ci

COPY src/ArchiveSystem.Frontend ./

ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}

RUN npm run build

FROM nginx:1.27-alpine AS runtime
WORKDIR /usr/share/nginx/html

COPY src/ArchiveSystem.Frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist ./

EXPOSE 8080

CMD ["nginx", "-g", "daemon off;"]
