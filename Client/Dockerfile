FROM node:22-alpine AS build
WORKDIR /app

COPY Client/package.json Client/package-lock.json ./
RUN npm ci

COPY Client/ ./

ENV VITE_API_BASE_URL=""
ENV VITE_HUB_PROTOCOL=json

RUN npm run build


FROM caddy:2-alpine AS runtime

COPY --from=build /app/dist /srv
COPY Caddyfile /etc/caddy/Caddyfile

EXPOSE 80 443