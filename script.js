document.addEventListener("DOMContentLoaded", () => {
    lucide.createIcons();
});

document.getElementById('scheduleForm').noValidate = true;

document.getElementById('scheduleForm').addEventListener('submit', function(event) {
    event.preventDefault();

    // Collect data for each section
    const professors = Array.from(document.querySelectorAll('#professorsTable tbody .professor')).map(prof => ({
        id: parseInt(prof.querySelector('.professorId').textContent.trim()),
        name: prof.querySelector('.professorName').textContent.trim()
    }));

    const courses = Array.from(document.querySelectorAll('#courseTable tbody .course')).map(course => ({
        id: parseInt(course.querySelector('.courseId').textContent.trim()),
        name: course.querySelector('.courseName').textContent.trim()
    }));

    const rooms = Array.from(document.querySelectorAll('#roomTable tbody .room')).map(room => ({
        name: room.querySelector('.roomName').textContent.trim(),
        lab: room.querySelector('.roomLab').checked,
        size: parseInt(room.querySelector('.roomSize').textContent.trim())
    }));  

    const groups = Array.from(document.querySelectorAll('#groupTable tbody .group')).map(group => ({
        id: parseInt(group.querySelector('.groupId').textContent.trim()),
        name: group.querySelector('.groupName').textContent.trim(),
        size: parseInt(group.querySelector('.groupSize').textContent.trim())
    }));

    const classes = Array.from(document.querySelectorAll('#classTable tbody .class')).map(cls => ({
        professor: parseInt(cls.querySelector('.classProfessorId').textContent.trim()),
        course: parseInt(cls.querySelector('.classCourseId').textContent.trim()),
        groups: [parseInt(cls.querySelector('.classGroupId').textContent.trim())],
        duration: parseInt(cls.querySelector('.classDuration').textContent.trim()),
        lab: cls.querySelector('.classLab').checked
    }));
    
    const scheduleData = [
        ...professors.map(prof => ({ prof })),
        ...courses.map(course => ({ course })),
        ...rooms.map(room => ({ room })),
        ...groups.map(group => ({ group })),
        ...classes.map(cls => ({ class: cls }))
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

// Functions to create objects
function addProfessor() {
    const id = document.getElementById('professorId').value.trim();
    const name = document.getElementById('professorName').value.trim();

    if (id && name) {
        const professorIds = Array.from(document.querySelectorAll('#professorsTable tbody .professorId')).map(el => el.textContent.trim());
        
        if (professorIds.includes(id)) {
            alert("Professor ID must be unique!");
            return;
        }

        // Check if ID and Name are provided
        if (!id || !name) {
            alert("Please fill in both ID and Name!");
            return;
        }

        // Check if ID is a valid number
        if (isNaN(id) || id <= 0) {
            alert("Professor ID must be a positive number!");
            return;
        }

        const tableBody = document.querySelector('#professorsTable tbody');
        const newRow = `
            <tr class="professor">
                <td class="professorId">${id}</td>
                <td class="professorName">${name}</td>
            </tr>
        `;
        tableBody.insertAdjacentHTML('beforeend', newRow);

        // Reset input fields
        document.getElementById('professorId').value = '';
        document.getElementById('professorName').value = '';
    } else {
        alert("Please fill in both ID and Name!");
    }
}

function addCourse() {
    const id = document.getElementById('courseId').value.trim();
    const name = document.getElementById('courseName').value.trim();

    if (isNaN(id) || id <= 0) {
        alert("Course ID must be a positive number!");
        return;
    }

    if (id && name) {
        const courseIds = Array.from(document.querySelectorAll('#courseTable tbody .courseId')).map(el => el.textContent.trim());

        if (courseIds.includes(id)) {
            alert("Course ID must be unique!");
            return;
        }

        const tableBody = document.querySelector('#courseTable tbody');
        const newRow = `
            <tr class="course">
                <td class="courseId">${id}</td>
                <td class="courseName">${name}</td>
            </tr>
        `;
        tableBody.insertAdjacentHTML('beforeend', newRow);

        // Reset input fields
        document.getElementById('courseId').value = '';
        document.getElementById('courseName').value = '';
    } else {
        alert("Please fill in both ID and Name!");
    }
}

function addRoom() {
    const name = document.getElementById('roomName').value.trim();
    const lab = document.getElementById('roomLab').checked;
    const size = document.getElementById('roomSize').value.trim();

    const roomNames = Array.from(document.querySelectorAll('#roomTable tbody .roomName')).map(el => el.textContent.trim());

    if (roomNames.includes(name)) {
        alert("Room name must be unique!");
        return;
    }

    if (!name || !size) {
        alert("Please fill in both Room Name and Room Size!");
        return;
    }

    if (isNaN(size) || size <= 0) {
        alert("Room size must be a positive number!");
        return;
    }

    const tableBody = document.querySelector('#roomTable tbody');
    const newRow = `
        <tr class="room">
            <td class="roomName">${name}</td>
            <td class="roomLab">${lab}</td>
            <td class="roomSize">${size}</td>
        </tr>
    `;
    tableBody.insertAdjacentHTML('beforeend', newRow);

    document.getElementById('roomName').value = '';
    document.getElementById('roomLab').checked = false;
    document.getElementById('roomSize').value = '';

}

function addGroup() {
    const id = document.getElementById('groupId').value.trim();
    const name = document.getElementById('groupName').value.trim();
    const size = document.getElementById('groupSize').value.trim();

    if (isNaN(id) || id <= 0) {
        alert("Group ID must be a positive number!");
        return;
    }

    if (isNaN(size) || size <= 0) {
        alert("Group size must be a positive number!");
        return;
    }

    if (id && name && size) {
        const groupIds = Array.from(document.querySelectorAll('#groupTable tbody .groupId')).map(el => el.textContent.trim());

        if (groupIds.includes(id)) {
            alert("Group ID must be unique!");
            return;
        }

        const tableBody = document.querySelector('#groupTable tbody');
        const newRow = `
            <tr class="group">
                <td class="groupId">${id}</td>
                <td class="groupName">${name}</td>
                <td class="groupSize">${size}</td>
            </tr>
        `;
        tableBody.insertAdjacentHTML('beforeend', newRow);

        document.getElementById('groupId').value = '';
        document.getElementById('groupName').value = '';
        document.getElementById('groupSize').value = '';
    } else {
        alert("Please fill in ID, Name and Size!");
    }
}

function addClass() {
    const profId = document.getElementById('classProfessorId').value.trim();
    const courseId = document.getElementById('classCourseId').value.trim();
    const groupId = document.getElementById('classGroupId').value.trim();
    const duration = document.getElementById('classDuration').value.trim();
    const lab = document.getElementById('classLab').checked;

    const classIds = Array.from(document.querySelectorAll('#classTable tbody .classProfessorId')).map(el => el.textContent.trim());

    if (!profId || !courseId || !groupId || !duration) {
        alert("Please fill in all fields for the class!");
        return;
    }

    if (isNaN(profId) || profId <= 0) {
        alert("Professor ID must be a positive number!");
        return;
    }

    if (isNaN(courseId) || courseId <= 0) {
        alert("Course ID must be a positive number!");
        return;
    }

    if (isNaN(groupId) || groupId <= 0) {
        alert("Group ID must be a positive number!");
        return;
    }

    if (isNaN(duration) || duration <= 0) {
        alert("Duration must be a positive number!");
        return;
    }

    const tableBody = document.querySelector('#classTable tbody');
    const newRow = `
        <tr class="class">
            <td class="classProfessorId">${profId}</td>
            <td class="classCourseId">${courseId}</td>
            <td class="classGroupId">${groupId}</td>
            <td class="classDuration">${duration}</td>
            <td class="classLab">${lab}</td>
        </tr>
    `;
    tableBody.insertAdjacentHTML('beforeend', newRow);

    document.getElementById('classProfessorId').value = '';
    document.getElementById('classCourseId').value = '';
    document.getElementById('classGroupId').value = '';
    document.getElementById('classDuration').value = '';
    document.getElementById('classLab').checked = false;

}
