
# Class Schedule Generator with Genetic Algorithm

This project is a web application that allows users to enter details for professors, courses, rooms, student groups, and classes via an HTML form. It saves the entered data to a JSON file, which can be used as input for a genetic algorithm-based class scheduling system.

## Features

- Form-based input for scheduling data: professor, course, room, group, and class details.
- Saves form data to a JSON file on the server.
- Ready for use with a genetic algorithm to generate optimized class schedules.

## Prerequisites

To get started, make sure you have the following installed:

- **Node.js** (for running the server)
- **Express.js**: Node.js framework for handling requests
- **Cors**: Middleware to handle Cross-Origin Resource Sharing

## Getting Started

### 1. Clone the Repository

Clone this repository to your local machine:
```bash
git clone https://github.com/username/ClassScheduleGA.git
```

### 2. Install Required Packages

Navigate to the project directory and install the required packages by running:
```bash
npm install express cors
```

### 3. Run the Server

Start the server by running:
```bash
node server.js
```

This will start the server at [http://localhost:3000](http://localhost:3000).

### 4. Open the HTML Form

Open `index.html` in your web browser to view the form and enter scheduling data.

### 5. Submit Form Data

- Fill out all required fields in each section (Professor, Course, Room, Group, Class).
- Click **Generate Schedule** to submit the form.
- The data will be sent to the server and saved to `user_data.json`.

> **Note**: Ensure the server is running before clicking the submit button to save data successfully.

### File Structure

- **index.html**: The main HTML file with the form for entering schedule data.
- **script.js**: JavaScript file to handle form submission and send data to the server.
- **server.js**: Node.js server file to receive and save data as a JSON file.

---

Let me know if you need any adjustments!
