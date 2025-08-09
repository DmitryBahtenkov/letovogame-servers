# Letovo Network Analysis Lab

Проект для обучения студентов работе с сетевыми протоколами и анализу трафика через Wireshark.

## Сервисы

### TCP File Server (порт 8080)
- Поддержка команд `READ` и `WRITE:текст`
- Чтение файла построчно с задержкой
- Запись данных в файл

### Web API Server (порт 5000)
- REST API для логирования
- GET `/` - получение логов из файла
- POST `/log` - запись логов в JSONL формате

## Docker развертывание

### Сборка и запуск всех сервисов:
```bash
docker-compose up -d
```

### Остановка:
```bash
docker-compose down
```

### Проверка логов:
```bash
docker logs tcp-file-server
docker logs web-api-server
```

## Использование

### TCP Server:
```bash
# Чтение файла
echo "READ" | nc <your-vps-ip> 8080

# Запись в файл
echo "WRITE:Новый текст" | nc <your-vps-ip> 8080
```

### Web API:
```bash
# Получение логов
curl http://<your-vps-ip>:5000/

# Отправка лога
curl -X POST http://<your-vps-ip>:5000/log \
  -H "Content-Type: application/json" \
  -d '{"message":"test","level":"info"}'
```

## Для VPS развертывания

1. Скопируйте все файлы проекта на VPS
2. Запустите: `docker-compose up -d`
3. Откройте порты 8080 и 5000 в фаерволе

## Для Wireshark

Студенты могут анализировать трафик обоих сервисов:
- TCP: `tcp.port == 8080`
- HTTP: `tcp.port == 5000` или `http`
- Видят различные протоколы и их особенности