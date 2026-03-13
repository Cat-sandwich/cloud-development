## Описание проекта

Проект представляет собой сервис для получения информации о сотрудниках с использованием кэширования Redis.

## Архитектура проекта

Решение состоит из нескольких проектов:

- **Employee.ApiService** – Web API сервис
- **Employee.AppHost** – Aspire orchestrator
- **Employee.ServiceDefaults** – общие настройки сервисов
- **Client.Wasm** – клиент

## Основная логика работы

1. Клиент отправляет запрос на получение сотрудника по `id`.
2. Сервис проверяет наличие данных в Redis.
3. Если данные есть в кэше, то они возвращаются из Redis.
4. Если данных нет, то сотрудник генерируется с помощью Bogus.

## Запуск проекта

1. Запустить проект **Employee.AppHost**.
2. Aspire Dashboard откроется автоматически.
3. В Dashboard будут запущены:
   - Redis
   - RedisInsight
   - ApiService
   - WebFrontend

## Пример работы приложения
<img width="1280" height="621" alt="image" src="https://github.com/user-attachments/assets/e37e467e-7436-438f-9879-e3f9f6e275cb" />
<img width="1280" height="275" alt="image" src="https://github.com/user-attachments/assets/a3d191bf-bf2d-454f-b212-c3efd56c92f9" />
<img width="1280" height="617" alt="image" src="https://github.com/user-attachments/assets/bf52b22e-c1f6-4d63-8d7f-a63237935e01" />
