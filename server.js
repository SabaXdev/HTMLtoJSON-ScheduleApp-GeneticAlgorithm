// server.js
const express = require('express');
const cors = require('cors');
const fs = require('fs');
const app = express();
const PORT = 3000;

app.use(cors());

app.use(express.json());

app.post('/save-data', (req, res) => {
    const data = req.body;

    // Save the JSON data to a file
    fs.writeFile('user_data.json', JSON.stringify(data, null, 2), (err) => {
        if (err) {
            console.error(err);
            res.status(500).json({ message: 'Failed to save data' });
        } else {
            res.status(200).json({ message: 'Data saved successfully' });
        }
    });
});

app.listen(PORT, () => {
    console.log(`Server is running on http://localhost:${PORT}`);
});
