# AI Fitness Coach 💪🤖

AI Fitness Coach is an intelligent fitness application that uses Artificial Intelligence to provide personalized workout plans, track user performance, and deliver smart fitness recommendations in real time.

## 🚀 Features

- AI-generated workout plans
- Real-time exercise tracking
- Personalized fitness recommendations
- BMI & calorie calculation
- Daily workout schedules
- Progress analytics
- User authentication
- Cloud database integration

## 🛠 Technologies Used

### Frontend
- Flutter
- Dart

### Backend
- Firebase
- REST APIs

### AI & Machine Learning
- Python
- TensorFlow / Scikit-learn
- OpenCV

### Database
- Firebase Firestore

---

## 🧩 System Architecture

The system consists of:

- Mobile Application
- AI Recommendation Engine
- Cloud Database
- Authentication System

---

## 📂 Project Structure

```bash
AI-Fitness-Coach/
│── lib/
│── assets/
│── models/
│── screens/
│── services/
│── widgets/
│── firebase/
│── README.md
```

---

# ⚙️ Installation Guide

## 2.1.1 Flutter Installation

AI Fitness Coach can be installed and run as a Flutter mobile application.  
The app supports Android development using Flutter and can be tested on an emulator or a physical Android device.

```bash
git clone https://github.com/ahmedmahmoud171325-hub/flutter.git
cd flutter
flutter pub get
flutter run
```

---

## 2.1.2 Running the App Development Version

### Prerequisites

1. Flutter SDK
2. Dart SDK
3. Android Studio or VS Code
4. Android Emulator or Physical Android Device

### Steps

1. Clone the Flutter repository.
2. Navigate to the project directory.
3. Install dependencies using:

```bash
flutter pub get
```

4. Run the application using:

```bash
flutter run
```

---

## 2.1.3 Backend Installation

The backend is developed using ASP.NET Core Web API (.NET 8.0).

### Install Dependencies

```bash
dotnet restore
```

### Run Backend Server

```bash
dotnet run
```

### Backend Dependencies

```bash
# Authentication & Security
BCrypt.Net-Next==4.1.0
Microsoft.AspNetCore.Authentication.JwtBearer==8.0.7
System.IdentityModel.Tokens.Jwt==7.5.1

# Database / ORM
Microsoft.EntityFrameworkCore==8.0.7
Microsoft.EntityFrameworkCore.SqlServer==8.0.7
Microsoft.EntityFrameworkCore.Tools==8.0.7

# JSON Handling
Newtonsoft.Json==13.0.4

# API Documentation
Swashbuckle.AspNetCore==6.5.0
```

---

## 2.1.4 Python AI Model Installation

The AI model can be installed and executed using the provided batch file.  
The script automatically installs all required Python dependencies and starts the FastAPI server used for:

- Real-time exercise detection
- Posture correction
- Workout analysis

The AI model can be tested locally and integrated with the Flutter application and backend API during runtime.

### Requirements

- Python 3.10+
- Conda Environment
- CUDA Support (Optional for GPU acceleration)

### Batch File

```bat
@echo off
title AI Fitness Coach - AI Model

echo ============================================
echo AI Fitness Coach - AI Model Startup
echo ============================================

echo Activating Conda Environment...
call conda activate ml

echo.
echo Installing Required Packages...
echo.

:: --- Core AI / Deep Learning ---
pip install torch==2.5.1
pip install torchvision==0.20.1
pip install torchaudio==2.5.1

:: --- Pose Estimation ---
pip install mediapipe==0.10.11

:: --- Numerical / Scientific ---
pip install numpy==2.0.1
pip install scikit-learn==1.8.0

:: --- Computer Vision ---
pip install opencv-python==4.10.0.84

:: --- Web Server (API) ---
pip install fastapi==0.136.1
pip install uvicorn==0.46.0

echo.
echo ============================================
echo Starting AI Model Server...
echo ============================================

cd /d D:\AI_coach\ai_model

uvicorn app:app --reload

pause
```
