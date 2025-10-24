# 🎵 Ekofy Capstone – Music Streaming & Creative Service Platform (Backend)

## 📘 Overview / Tổng quan  
**Ekofy** is a full-stack music streaming and creative service platform — inspired by **Spotify** and **SoundBetter**.  
The backend handles **secure streaming**, **offline playback**, **AI-based music discovery**, and **commission services** for artists.

**Ekofy** là nền tảng kết hợp giữa **streaming âm nhạc** và **dịch vụ sáng tạo âm nhạc theo yêu cầu**, lấy cảm hứng từ **Spotify** và **SoundBetter**.  
Hệ thống backend hỗ trợ **phát nhạc an toàn**, **nghe offline**, **tìm kiếm ngữ nghĩa bằng AI**, và **đặt hàng âm nhạc theo yêu cầu**.

---

## 🚀 Key Features / Tính năng chính
- 🎧 **HLS + AES-128 Secure Streaming**  
  Adaptive HLS with AES-128 DRM encryption, playable only via authenticated clients.  
  _Phát nhạc qua HLS mã hóa AES-128, chỉ khả dụng trên client được xác thực._
- 💾 **EmySound Storage and AWS S3 Integration**  
  Custom object storage & CDN service for encrypted media delivery.  
  _Tích hợp hệ thống lưu trữ EmySound để quản lý, phân phối file nhạc và metadata an toàn._
- 🔍 **Audio Fingerprinting**  
  Uses FFmpeg and custom ML models to generate unique acoustic fingerprints for duplicate detection.  
  _Tạo “vân tay âm thanh” để nhận dạng bài hát và phát hiện trùng lặp._
- 🧠 **Semantic Search Engine**  
  Natural language queries → feature vectors → mood/genre matching.  
  _Tìm kiếm nhạc bằng ngôn ngữ tự nhiên (ví dụ: “bài ballad buồn cho tâm trạng thất tình”)._
- 💸 **Royalty Management**  
  Automatic royalty split (recording/publishing) with monthly aggregation jobs via Hangfire.  
  _Tự động chia tiền bản quyền ghi âm và xuất bản hằng tháng._
- 🔒 **JWT Authentication & Role-Based Access**  
  Secure API with user roles: Listener, Artist, Moderator, Admin.  
  _Xác thực JWT và phân quyền người dùng._
- 📀 **Offline Mode (DRM-like)**  
  Encrypted downloads playable only within Ekofy mobile app.  
  _Nghe offline an toàn, không thể trích xuất file._

---

## 🏗️ Architecture / Kiến trúc
| Layer | Technology | Description / Mô tả |
|:--|:--|:--|
| **Backend API** | ASP.NET Core 8 Web API | REST & GraphQL endpoints |
| **Database** | MongoDB | Main data store for users, tracks, requests |
| **Cache** | Redis | Caching & session management |
| **Storage** | EmySound, AWS S3 + Cloudfront | Secure encrypted media hosting |
| **Fingerprinting** | FFmpeg + Librosa (Python) | Audio feature extraction & identity |
| **Semantic Search** | gRPC + Embedding Model (mxbai) | Text-to-feature-vector conversion |
| **Background Jobs** | Hangfire | Royalty calculation, cleanup tasks |
| **Realtime** | SignalR | Streaming stats, notifications |
| **DevOps** | Docker, Nginx, GitHub Actions | CI/CD automation & HTTPS reverse proxy |

---

## 💻 Development Setup / Hướng dẫn phát triển
**Prerequisites / Yêu cầu:**  
- Docker & Docker Compose  
- .NET 8 SDK  
- MongoDB, Redis containers  
- EmySound credentials (API Key, Storage URL or Docker for community)  

```bash
# Clone repository
git clone https://github.com/<your-username>/EkofyCapstone.git
cd EkofyCapstone

# Copy and configure environment
cp .env.example .env
# Fill in: DB_CONNECTION, REDIS_URL, EMYSOUND_API_KEY, JWT_SECRET, etc.

# Build and run
docker compose up -d --build
