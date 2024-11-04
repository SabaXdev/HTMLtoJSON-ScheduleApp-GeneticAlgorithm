// server.js
const express = require('express');
const cors = require('cors');
const fs = require('fs');
const app = express();
const { exec } = require('child_process')
const PORT = 3000;

app.use(cors());

app.use(express.json());

app.post('/save-data', (req, res) => {
    const data = req.body;
    const jsonFilePath = './GaSchedule.Console/user_data_new.json';

    // Save the JSON data to a file
    fs.writeFile(jsonFilePath, JSON.stringify(data, null, 2), (err) => {
        if (err) {
            console.error(err);
            res.status(500).json({ message: 'Failed to save data' });
        } else {
            res.status(200).json({ message: 'Data saved successfully' });
        }
    });

    // Change directory to GaSchedule.Console and run dotnet build and dotnet run
    exec(`cd GaSchedule.Console && dotnet build && dotnet run`, (error, stdout, stderr) => {
        if (error) {
            console.error(`Error: ${error.message}`);
            return res.status(500).send('Failed to run .NET project');
        }
        if (stderr) {
            console.error(`Stderr: ${stderr}`);
        }
        console.log(`Stdout: ${stdout}`);
        
        res.send('Schedule generation process initiated');
    });
});

app.listen(PORT, () => {
    console.log(`Server is running on http://localhost:${PORT}`);
});
