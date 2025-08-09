# TCP File Server

TCP-сервер для обучения студентов работе с сетевыми протоколами и Wireshark.

## Функциональность

- Поддержка команд `READ` и `WRITE:текст`
- Чтение файла построчно с задержкой
- Запись данных в файл
- Docker-контейнеризация для простого развертывания

## Локальный запуск

```bash
dotnet run
```

## Docker развертывание

### Сборка и запуск:
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
```

## Использование

### Чтение файла:
```bash
echo "READ" | nc <your-vps-ip> 8080
```

### Запись в файл:
```bash
echo "WRITE:Новый текст" | nc <your-vps-ip> 8080
```

## Для VPS развертывания

1. Скопируйте файлы на VPS:
   - Dockerfile
   - docker-compose.yml
   - TCP.csproj
   - Program.cs

2. Запустите на VPS:
```bash
docker-compose up -d
```

3. Проверьте что порт 8080 открыт в фаерволе

## Для Wireshark

Студенты могут анализировать трафик:
- Фильтр: `tcp.port == 8080`
- Видят команды и данные в TCP-пакетах
- Могут перехватывать передачу файлов