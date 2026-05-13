## Описание проекта

Проект представляет собой распределённую систему для получения информации о сотрудниках с использованием кэширования Redis и балансировки нагрузки по алгоритму Query Based, а также используются очереди сообщений и объектное хранилище.

## Архитектура проекта

Решение состоит из нескольких проектов:

- **Employee.ApiService** – Web API сервис
- **Employee.ApiGateway** – API Gateway на базе Ocelot
- **Employee.FileService** – сервис обработки сообщений и сохранения файлов
- **Employee.AppHost** – Aspire orchestrator
- **Employee.ServiceDefaults** – общие настройки сервисов
- **Client.Wasm** – клиент

## Основная логика работы

1. Клиент отправляет запрос в API Gateway (`/api/employee?id={id}`).
2. API Gateway (Ocelot) принимает запрос и передаёт его в один из сервисов генерации.
3. Выбор сервиса осуществляется с помощью кастомного балансировщика `QueryBasedLoadBalancer`.
4. Сервис:
   - проверяет наличие сотрудника в Redis,
   - при отсутствии данных генерирует нового сотрудника,
   - сохраняет данные в Redis,
   - отправляет сообщение в очередь SQS.
5. `Employee.FileService` получает сообщение из очереди.
6. Данные сотрудника сериализуются в JSON-файл.
7. Файл сохраняется в S3-хранилище LocalStack.
  
## Оркестрация сервисов

С помощью Aspire настроен запуск нескольких реплик сервиса генерации:

- generator-1 → http://localhost:5201  
- generator-2 → http://localhost:5202  
- generator-3 → http://localhost:5203  

## Запуск проекта

1. Запустить проект **Employee.AppHost**.
2. Aspire Dashboard откроется автоматически.
3. В Dashboard будут запущены:
   - Localstack
   - Redis
   - Redis Commander
   - 3 реплики Employee.ApiService
   - API Gateway
   - Employee.FileService
   - WebFrontend
   
## Пример работы приложения
<img width="1580" height="895" alt="image" src="https://github.com/user-attachments/assets/b318b6f5-6cf0-4806-8aee-a31d3865b2ab" />
<img width="1474" height="903" alt="image" src="https://github.com/user-attachments/assets/f481685b-e63a-46fa-9364-a1635c6a84b5" />
<img width="1086" height="124" alt="image" src="https://github.com/user-attachments/assets/efff9ebe-d106-46fe-a3ba-e89a08ab6681" />
<img width="1122" height="303" alt="image" src="https://github.com/user-attachments/assets/d2f252a1-630e-47f1-81f3-d91a76cc3b56" />





