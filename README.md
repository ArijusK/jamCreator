# JamCreator  
**Real-Time Collaborative Music Platform**

JamCreator is a web application that allows multiple users to listen to music together in shared rooms, interact through chat, and collaboratively control a music queue.

---

## Features

- Real-time multi-user rooms  
- Chat system using SignalR  
- Collaborative music queue with vote-to-skip functionality  
- User tracking (who added which song)  
- Backend built with C# and ASP.NET  
- Database integration using Entity Framework Core and PostgreSQL  

---

## Tech Stack

- **Backend:** C#, ASP.NET Core  
- **Real-time communication:** SignalR  
- **Database:** PostgreSQL + Entity Framework Core  
- **Frontend:** Blazor (WebAssembly)  
- **Other:** Docker (for database setup)

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/ArijusK/jamCreator.git
cd jamCreator
```

### 2. Start the database (Docker)

Make sure Docker Desktop is running, then:

```bash
docker compose up -d
```

This will start a PostgreSQL database on port 5432.

### 3. Run the application

Navigate to the main project folder (JamCreator folder with JamCreator.csproj file) and run:

```bash
dotnet run
```
On first run:

the database will be created automatically
migrations will be applied

### 4. Open the app

Open your browser and go to:

https://localhost:xxxx

(Replace with the port shown in your terminal if different)

---

## Project Structure
 - JamCreator/ – main server application
 - JamCreator.Client/ – frontend (Blazor WebAssembly)
 - JamCreator.Shared/ – shared models and interfaces
 - tests/ – testing project
  
## Notes
 - The application uses SignalR hubs for real-time features (chat and music sync).
 - Database connection is configured in appsettings.json.
 - Docker is used only for PostgreSQL to simplify setup.

## Future Improvements
 - Spotify API integration
 - Improved synchronization of playback across users
 - Enhanced UI/UX

## Members and their github usernames:

* Arijus Kaminskas
  
  > ArijusK
  
* Vilius Gylys

  >Zuvautojas
  
* Deividas Matonis

  >DuckWarrior0808

* Justinas Jarmalavičius

  > Just1naz

* Karolis Palubinskas

  >Karolis814

 
    
  
  
  
    

  
