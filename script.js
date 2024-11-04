document.getElementById('scheduleForm').addEventListener('submit', function(event) {
    event.preventDefault();

    const professor = {
        id: parseInt(document.getElementById('professorId').value),
        name: document.getElementById('professorName').value
    };

    const course = {
        id: parseInt(document.getElementById('courseId').value),
        name: document.getElementById('courseName').value
    }

    const room = {
        name: document.getElementById('roomName').value,
        lab: document.getElementById('roomLab').checked,
        size: parseInt(document.getElementById('roomSize').value)
    };

    const group = {
        id: parseInt(document.getElementById('groupId').value),
        name: document.getElementById('groupName').value,
        size: parseInt(document.getElementById('groupSize').value)
    };

    const courseClass = {
        professor: parseInt(document.getElementById('classProfessorId').value),
        course: parseInt(document.getElementById('classCourseId').value),
        groups: [parseInt(document.getElementById('classGroupId').value)],
        duration: parseInt(document.getElementById('classDuration').value),
        lab: document.getElementById('classLab').checked
    };

    const scheduleData = [
        { "prof": professor },
        { "course": course },
        { "room": room },
        { "group": group },
        { "class": courseClass }
    ];

    fetch('http://localhost:3000/save-data', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(scheduleData)
    })
    .then(response => response.json())
    .then(data => {
        console.log("Success:", data);
        alert("Data saved to JSON file successfully!");
    })
    .catch(error => {
        console.error('Error:', error);
        alert("An error occurred.");
    });
});
