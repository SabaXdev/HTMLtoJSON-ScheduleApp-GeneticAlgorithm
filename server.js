const express = require('express');
const cors = require('cors');
const fs = require('fs');
const app = express();
const { exec } = require('child_process');
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
        console.log('Data saved successfully. Now running .NET project...');

        // Run the .NET commands only after saving the data
        exec(`cd GaSchedule.Console && dotnet build && dotnet run`, (error, stdout, stderr) => {
            if (error) {
                console.error('Error running .NET project:', error);
                return res.status(500).json({ message: 'Failed to run .NET project' });
            }
            if (stderr) {
                console.error('Stderr:', stderr);
            }
            console.log('Stdout:', stdout);
            // Send a response after successfully running the process
            // res.status(200).json({ message: 'Schedule generation process initiated' });
        });
    });
});

app.listen(PORT, () => {
    console.log(`Server is running on http://localhost:${PORT}`);
});
