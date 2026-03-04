# RasterToSpline.Core

## Как скачать готовые DLL через GitHub Actions (без Visual Studio)

### Шаг 1 — Создайте бесплатный аккаунт на GitHub
Перейдите на **https://github.com** → Sign up (если ещё нет аккаунта).

---

### Шаг 2 — Создайте новый репозиторий
1. Нажмите **+** (в правом верхнем углу) → **New repository**
2. Имя: `RasterToSpline` (или любое другое)
3. Тип: **Private** (никто кроме вас не увидит)
4. Нажмите **Create repository**

---

### Шаг 3 — Загрузите файлы
В новом репозитории нажмите **uploading an existing file** и загрузите **все файлы из этого архива**, сохраняя структуру папок:

```
RasterToSpline.sln
src/
    ContourTracer.cs
    ContourTracerExtensions.cs
    ContourTracerOptions.cs
    RasterToSpline.Core.csproj
.github/
    workflows/
        build.yml
```

Нажмите **Commit changes**.

---

### Шаг 4 — Дождитесь сборки (≈ 3–5 минут)
1. Перейдите во вкладку **Actions** в вашем репозитории
2. Вы увидите запущенный workflow **"Build RasterToSpline.Core DLL"**
3. Дождитесь зелёной галочки ✅

---

### Шаг 5 — Скачайте DLL
1. Кликните на завершённый workflow
2. Внизу страницы найдите раздел **Artifacts**
3. Нажмите **RasterToSpline-DLLs-x64** — скачается ZIP
4. Распакуйте — внутри будут все 4 файла:
   - `RasterToSpline.Core.dll`
   - `OpenCvSharp.dll`
   - `OpenCvSharp.runtime.win.dll`
   - `OpenCvSharpExtern.dll` (~90 МБ, нативный OpenCV)

---

### Шаг 6 — Разместите DLL в 3ds Max
Скопируйте все 4 файла в папку:
```
<3ds Max 2024>\scripts\RasterToSpline\bin\
```
