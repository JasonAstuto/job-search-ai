FROM node:26-bullseye-slim AS deps
WORKDIR /app

COPY ["src/Frontend/package.json", "src/Frontend/package-lock.json*", "./"]
RUN npm install

COPY src/Frontend/ .

EXPOSE 3000
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]
