using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;

using GaSchedule.Model;

namespace GaSchedule
{
    public class HtmlOutput
    {
        private const int ROOM_COLUMN_NUMBER = Constant.DAYS_NUM + 1;
        private const int ROOM_ROW_NUMBER = Constant.DAY_HOURS + 1;

		private const string COLOR1 = "#319378";
		private const string COLOR2 = "#CE0000";
		private static char[] CRITERIAS = { 'R', 'S', 'L', 'P', 'G'};
		private static string[] OK_DESCR = { "Current room has no overlapping", "Current room has enough seats", "Current room with enough computers if they are required",
			"Professors have no overlapping classes", "Student groups has no overlapping classes" };
		private static string[] FAIL_DESCR = { "Current room has overlapping", "Current room has not enough seats", "Current room with not enough computers if they are required",
			"Professors have overlapping classes", "Student groups has overlapping classes" };
		private static string[] PERIODS = {"", "9 - 10", "10 - 11", "11 - 12", "12 - 13", "13 - 14", "14 - 15", "15 - 16", "16 - 17", "17 - 18", "18 - 19", "19 - 20", "20 - 21" };
		private static string[] WEEK_DAYS = { "MON", "TUE", "WED", "THU", "FRI"};

		private static string GetTableHeader(Room room)
		
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("<tr><th style='border: .1em solid black' scope='col' colspan='2'>Room: ");
			sb.Append(room.Name);
			sb.Append("</th>\n");
			foreach(string weekDay in WEEK_DAYS)
			sb.Append("<th style='border: .1em solid black; padding: .25em; width: 15%' scope='col' rowspan='2'>").Append(weekDay).Append("</th>\n");
			sb.Append("</tr>\n");
			sb.Append("<tr>\n");
			sb.Append("<th style='border: .1em solid black; padding: .25em'>Lab: ").Append(room.Lab ? "Yes" : "No").Append("</th>\n");
			sb.Append("<th style='border: .1em solid black; padding: .25em'>Seats: ").Append(room.NumberOfSeats).Append("</th>\n");
			sb.Append("</tr>\n");
			return sb.ToString();
		}

		private static Dictionary<Point, string[]> GenerateTimeTable(Schedule solution, Dictionary<Point, int[]> slotTable)
		{
			int numberOfRooms = solution.Configuration.NumberOfRooms;
			int daySize = Constant.DAY_HOURS * numberOfRooms;

			int ci = 0;
			var classes = solution.Classes;

			var timeTable = new Dictionary<Point, string[]>();
			foreach (var cc in classes.Keys)
			{
				// coordinate of time-space slot
				var reservation = Reservation.GetReservation(classes[cc]);
				int dayId = reservation.Day + 1;
				int periodId = reservation.Time + 1;
				int roomId = reservation.Room;

				var key = new Point(periodId, roomId);
				var roomDuration = slotTable.ContainsKey(key) ? slotTable[key] : null;
				if (roomDuration == null)
				{
					roomDuration = new int[ROOM_COLUMN_NUMBER];
					slotTable[key] = roomDuration;
				}
				roomDuration[dayId] = cc.Duration;
				for (int m = 1; m < cc.Duration; ++m)
				{
					var nextRow = new Point(periodId + m, roomId);
					if (!slotTable.ContainsKey(nextRow))
						slotTable.Add(nextRow, new int[ROOM_COLUMN_NUMBER]);
					if (slotTable[nextRow][dayId] < 1)
						slotTable[nextRow][dayId] = -1;
				}

				var roomSchedule = timeTable.ContainsKey(key) ? timeTable[key] : null;

				// Adding Class Information
				var sb = new StringBuilder();
				if (roomSchedule == null) {
					roomSchedule = new string[ROOM_COLUMN_NUMBER];
					timeTable[key] = roomSchedule;
				}
				sb.Append(cc.Course.Name).Append("<br />").Append(cc.Professor.Name).Append("<br />");
				sb.Append(string.Join("/", cc.Groups.Select(grp => grp.Name).ToArray()));
				sb.Append("<br />");
				if (cc.LabRequired)
					sb.Append("Lab<br />");

				// Adding Criteria Indicators
				for(int i=0; i< CRITERIAS.Length; ++i)
				{
					sb.Append("<span style='color:");
					if(solution.Criteria[ci + i])
					{
						sb.Append(COLOR1).Append("' title='");
						sb.Append(OK_DESCR[i]);
					}
					else
					{
						sb.Append(COLOR2).Append("' title='");
						sb.Append(FAIL_DESCR[i]);
					}
					sb.Append("'> ").Append(CRITERIAS[i]);
					sb.Append(" </span>");
				}
				roomSchedule[dayId] = sb.ToString();
				ci += CRITERIAS.Length;
			}
			return timeTable;
		}

		private static string GetHtmlCell(string content, int rowspan)
		{
			if (rowspan == 0)
				return "<td></td>";

			if (content == null)
				return "";

			StringBuilder sb = new StringBuilder();
			if (rowspan > 1)
				sb.Append("<td style='border: .1em solid black; padding: .25em' rowspan='").Append(rowspan).Append("'>");
			else
				sb.Append("<td style='border: .1em solid black; padding: .25em'>");

			sb.Append(content);
			sb.Append("</td>");
			return sb.ToString();
		}

		// Function to parse room data
        public static Room ParseRoom(Dictionary<string, JsonElement> roomData)
        {
            // Extract and parse room details from JSON data
            var name = roomData.ContainsKey("name") ? roomData["name"].GetString() : null;
            var lab = roomData.ContainsKey("lab") ? roomData["lab"].GetBoolean() : false;
            var size = roomData.ContainsKey("size") ? roomData["size"].GetInt32() : 0;

            // Return a new Room object
            return new Room(name, lab, size);
        }


		public static string GetResult(Schedule solution)
		{
			StringBuilder sb = new StringBuilder();
			int nr = solution.Configuration.NumberOfRooms;
    		
			var slotTable1 = new Dictionary<Point, int[]>();    		
			var timeTable1 = GenerateTimeTable(solution, slotTable1);

			// Read and Deserialize the room data from the JSON file
			var filePath = "user_data_new.json";
			var data = JsonSerializer.Deserialize<List<Dictionary<string, Dictionary<string, JsonElement>>>>(File.ReadAllText(filePath));

			// Create a dictionary to hold roomId -> roomName
			var roomData = new Dictionary<int, string>();

			int setRoomID = 0;

			// Create room objects and populate roomData
			foreach (var item in data)
			{
				foreach (var obj in item)
				{

					if (obj.Key == "room")
					{
						var roomObjectName = obj.Value.ContainsKey("name") ? obj.Value["name"].GetString() : null;
						var roomIdFromTimeSlot = setRoomID;

						if (!roomData.ContainsKey(roomIdFromTimeSlot))
						{

							if (roomObjectName != null)
							{
								roomData[roomIdFromTimeSlot] = roomObjectName; // Map room ID to room name
							}
							else
							{
								Console.WriteLine("[DEBUG] Room name is null. Skipping.");
							}
						}
						setRoomID++;
						
					}
				}
			}
			// Final log to check roomData content
			Console.WriteLine("[DEBUG] Final roomData contents:");
			foreach (var room in roomData)
			{
				Console.WriteLine($"RoomId: {room.Key}, RoomName: {room.Value}");
			}
			
			var serializableTimeTable = timeTable1.ToDictionary(
				kvp => $"{kvp.Key.X},{kvp.Key.Y}", // Convert Point to "x,y" string
		        kvp => kvp.Value.Select(v => HttpUtility.HtmlEncode(v?.ToString() ?? string.Empty)).ToArray() // Escape HTML

			);

			var serializableSlotTable = slotTable1.ToDictionary(
				kvp => $"{kvp.Key.X},{kvp.Key.Y}", // Convert Point to "x,y" string
				kvp => kvp.Value.Select(v => v.ToString()).ToArray() // Convert int[] to string[]
			);

    		// Serialize timeTable to JSON
    		string timeTableJson = JsonSerializer.Serialize(serializableTimeTable);
			string slotTableJson = JsonSerializer.Serialize(serializableSlotTable);

			// Append timeTable1 to the StringBuilder
			sb.Append(@"
				<div id='timeTableData' data-timeTable='");
			sb.Append($"{timeTableJson}");
			sb.Append(@"'></div>");

			// Append slotTable1 to the StringBuilder
			sb.Append(@"
				<div id='slotTableData' data-slotTable='");
			sb.Append($"{slotTableJson}");
			sb.Append(@"'></div>");

			sb.Append(@"
    			<div id='roomNamesData' style='display:none;'>");
			foreach (var timeSlot in timeTable1)
			{
				var key = $"{timeSlot.Key.X},{timeSlot.Key.Y}";
				// Extract the roomId from the timeSlot key
				var roomIdFromTimeSlot = timeSlot.Key.Y;

				if (roomData.ContainsKey(roomIdFromTimeSlot))
				{	
					
					var roomName = roomData[roomIdFromTimeSlot];
					sb.Append($@"
						<div>Room {roomIdFromTimeSlot}: {roomName}</div>");
				}
				else
				{	
			        Console.WriteLine($"[DEBUG] Missing roomId: {roomIdFromTimeSlot}, Key: {key}");
			        Console.WriteLine($"[DEBUG] TimeSlot: {timeSlot.Key} -> {string.Join(", ", timeSlot.Value)}");
					// Handle case where roomId is missing in roomData (optional)
					sb.Append($@"
						<div>Room {roomIdFromTimeSlot}: Unknown Room</div>");
				}
			}
			sb.Append(@"
				</div>");


            sb.Append(@"
			    <div class='buttons' style='margin-bottom: 1em; display: flex; align-items: center;'>
            	<label for='searchBy'>Search By: </label>
            	<select id='searchBy' name='searchBy' style='padding: 0.5em; margin-right: 1em;'>
            		<option value='professor'>Professor</option>
            		<option value='group'>Group</option>
            		<option value='course'>Course</option>
            	</select>
            		<input type='text' id='searchTerm' placeholder='Enter search term' style='padding: 0.5em; margin-right: 1em; width: 250px;'>
            		<button id='searchBtn' style='background-color: #121927; padding: 10px 15px; color: white; border: none; border-radius: 0.25rem; cursor: pointer; font-size: 1rem; margin-right: 2px;'>
						Search
					</button>
					<button id='downloadBtn' style='padding: 10px 15px; background-color: #121927; color: white; border: none; border-radius: 0.25rem; cursor: pointer; font-size: 1rem;'>
						Download PDF
					</button>
				</div>

				<style>
					div {
						page-break-before: always;
					}

					.page-break {
						page-break-after: always;  /* Force a page break after each div */
						display: block;  /* Ensure that divs are block elements */
						overflow: visible;  /* Ensure content doesn't overflow or hide */
						min-height: 100%;  /* Stretch divs to fill the page */
					}

					.page-container {
						display: flex;
						flex-wrap: wrap;
						justify-content: space-between;
					}

					.page-container > div {
						width: 49%;  /* Allow two tables per page */
						margin-bottom: 20px;
					}

					#room_ {
						max-height: 90%; /* Prevent overflowing, allowing space for another room */
						overflow: hidden; /* Hide overflow if necessary */
					}

					button:hover {
						background-color: #1a263c;
						opacity: 0.9;
						transform: scale(1.05); /* Slightly grow the button */
						box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); /* Add a subtle shadow */
					}
				 </style>

				<!-- Add the script for html2pdf.js -->
				<script src='https://cdnjs.cloudflare.com/ajax/libs/html2pdf.js/0.9.2/html2pdf.bundle.min.js'></script>


				<script>
					function downloadPDF() {
						// Clone the document body to avoid altering the original
						const clonedBody = document.body.cloneNode(true);

						// Remove the div with the class 'buttons'
						const buttonsDiv = clonedBody.querySelector('.buttons');
						if (buttonsDiv) {
							buttonsDiv.remove();
						}

						// Reset the styles of cells that were highlighted during the search
						const styledCells = clonedBody.querySelectorAll('td[style]');
						styledCells.forEach(cell => {
						    const currentBorder = cell.style.border; 	// Save the current border style
							cell.removeAttribute('style'); 				// Remove inline styles
							if (currentBorder) {
								cell.style.border = currentBorder;  	// Restore the border style
							}
						});

						clonedBody.style.minHeight = '100%'; // Force content to stretch to fill the page	

						// Check if there's at least one table in the content before rendering the page
						const hasTable = clonedBody.querySelector('table');
						if (!hasTable) {
							console.log('No content to render on this page, skipping...');
							return; // Skip this empty content page
						}	
						
						// Define options for the PDF
						const options = {
							margin: 0.5,                       				  		// Margins in inches
							filename: 'timetable.pdf',         						// PDF file name
							html2canvas: { scale: 3, logging: true },         		// Increase quality by scaling up canvas
							jsPDF: { unit: 'in', format: 'letter', orientation: 'landscape' },
							pagebreak: { mode: 'avoid-all', after: '.page-break'}
						};

						// Generate the PDF using the modified content
						html2pdf().set(options).from(clonedBody).save();
					}


					// Function to perform search and generate a new table for a specific professor
					function performSearch() {
						// Get the search criteria values
						var searchBy = document.getElementById('searchBy').value.trim();
						var searchTerm = document.getElementById('searchTerm').value.trim();

						if (!searchTerm) {
							alert('Please enter a search term.');
							return;
						}

						const filteredData = [];
						const timeTableElement = document.getElementById('timeTableData');
						const timeTableJson = timeTableElement.getAttribute('data-timeTable');
						
						const slotTableElement = document.getElementById('slotTableData');
						const slotTableJson = slotTableElement.getAttribute('data-slotTable');

						const timeTable = JSON.parse(timeTableJson);
						const slotTable = JSON.parse(slotTableJson);
						console.log('Parsed timeTable:', timeTable);
						console.log('Parsed slotTable:', slotTable);

						//roomSchedule = timeTable[key] = value = array of strings()

						// Parse room names from the roomNamesData div
						const roomNamesDataElement = document.getElementById('roomNamesData');
						const roomNamesData = {};
						const roomDivs = roomNamesDataElement.querySelectorAll('div');
						roomDivs.forEach((div) => {
							const match = div.textContent.match(/Room (\d+): (.+)/);
							if (match) {
								const roomId = parseInt(match[1], 10); // Extract room ID
								const roomName = match[2].trim(); // Extract room name
								roomNamesData[roomId] = roomName;
							}
						});

						if (searchBy === 'professor') {
        					const professorName = searchTerm;

							for (const [key, value] of Object.entries(timeTable)) {
								const [time, roomId] = key.split(',').map(Number);
								const startHour = 8 + time; // Adjusted based on time (9 means class starts at 17)

								const timeSlot = slotTable[key]; // Get the time slots array for the current key

								for (let day = 1; day < value.length; day++) {
									if (value[day] && value[day].toLowerCase().includes(professorName.toLowerCase())) {
										const [course, prof, group, ...rest] = value[day].split('&lt;br /&gt;');
										let lab = 'No';
										for (const part of rest) {
											if (part.trim() === 'Lab') {
												lab = 'Yes';
												break;
											}
										}
										
										const duration = Number(timeSlot[day]); // This represents the duration for the day (e.g., 3 hours on Tuesday)
										const endHour = startHour + duration; // Calculate endHour based on duration
										
										// Fetch room name using roomId
										const roomName = roomNamesData[roomId] || `Unknown Room (ID: ${roomId})`;

										filteredData.push({
											day: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'][day - 1],
											time: `${startHour}:00 - ${endHour}:00`,
											course: course || '',
											professor: prof || '',
											group: group || '',
											room: roomName,
											lab: lab
										});
										console.log('filteredData:', filteredData);
									}
								}
							}

							if (filteredData.length === 0) {
								alert(`No classes found for Professor ${professorName}.`);
								return;
							}

							// Call the createProfessorTimetable function and pass professorName and filteredData
							createTimetable('Professor', searchTerm, filteredData);
						
						} else if (searchBy === 'course') {
							const courseName = searchTerm;

							for (const [key, value] of Object.entries(timeTable)) {
								const [time, roomId] = key.split(',').map(Number);
								const startHour = 8 + time;

								const timeSlot = slotTable[key];

								for (let day = 1; day < value.length; day++) {
									if (value[day] && value[day].toLowerCase().includes(courseName.toLowerCase())) {
										const [course, prof, group, ...rest] = value[day].split('&lt;br /&gt;');
										let lab = 'No';
										for (const part of rest) {
											if (part.trim() === 'Lab') {
												lab = 'Yes';
												break;
											}
										}
										
										const duration = Number(timeSlot[day]);
										const endHour = startHour + duration;
										
										const roomName = roomNamesData[roomId] || `Unknown Room (ID: ${roomId})`;

										filteredData.push({
											day: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'][day - 1],
											time: `${startHour}:00 - ${endHour}:00`,
											course: course || '',
											professor: prof || '',
											group: group || '',
											room: roomName,
											lab: lab
										});
										console.log('filteredData:', filteredData);
									}
								}
							}

							if (filteredData.length === 0) {
								alert(`No classes found for Course ${courseName}.`);
								return;
							}
							createTimetable('Course', searchTerm, filteredData);
						}
						
						else {
							alert('Other search criteria not implemented yet.');
						}

					}

					// Function to create a timetable for a specific professor
					function createTimetable(type, name, filteredData) {
					    // Check if the name exists in the filteredData based on the type (Professor or Course)
						const existsInData = filteredData.some(entry =>
							(type === 'Professor' && entry.professor === name) ||
							(type === 'Course' && entry.course === name)
						);

						// If name is not found, stop the function
						if (!existsInData) {
							alert(`${type} ${name} not found in the data.`);
							return;
						}

						function clearBodyExceptButtons() {
							// Get the buttons div
							let buttonsDiv = document.querySelector('.buttons');
							
							// Loop through all child elements of body and remove them except for the buttons div
							Array.from(document.body.children).forEach(child => {
								if (child !== buttonsDiv) {
									document.body.removeChild(child);
								}
							});
						}

						// Clear everything except the buttons
						clearBodyExceptButtons();

						// Create or clear the result section
						let resultSection = document.getElementById('resultSection');
						if (!resultSection) {
							resultSection = document.createElement('div');
							resultSection.id = 'resultSection';
							resultSection.style.marginTop = '1em';
							document.body.appendChild(resultSection); // Append to the body or a specific container
						}
						resultSection.innerHTML = ''; // Clear previous results

						// Define the table
						const table = document.createElement('table');
						table.style.borderCollapse = 'collapse';
						table.style.width = '95%';

						// Create the header row
						const headerRow = document.createElement('tr');

						// Add the type (Professor/Course) name as the first header cell
						const headerCell = document.createElement('th');
						headerCell.style.border = '.1em solid #ddd';
						headerCell.style.padding = '.25em';
						headerCell.textContent = `${type}: ${name}`;
						headerCell.style.backgroundColor = '#2C2F33'; // Dark gray background
						headerCell.style.color = '#FFFFFF'; // White text
						headerCell.style.fontSize = '1.3em'; // Customize font size
						headerCell.style.fontWeight = 'bold'; // Bold text for emphasis
						headerCell.style.textAlign = 'center';
						headerRow.appendChild(headerCell);

						// Add headers for each day
						['MON', 'TUE', 'WED', 'THU', 'FRI'].forEach(day => {
							const th = document.createElement('th');
							th.style.backgroundColor = '#7a7a7a';
                    		th.style.color = 'white';
							th.style.border = '.1em solid #ddd';
							th.style.padding = '.25em';
							th.style.width = '17%';
							th.textContent = day;
							th.style.textAlign = 'center';
							headerRow.appendChild(th);
						});
						table.appendChild(headerRow);

						console.log('filteredData in TimeTable:', filteredData);

						// Create rows for each time slot (9-10, 10-11, ..., 20-21)
						for (let hour = 9; hour <= 20; hour++) {
							const row = document.createElement('tr');

							// Add the time slot as the first cell
							const timeCell = document.createElement('th');
							timeCell.style.backgroundColor = '#7a7a7a';
                    		timeCell.style.color = 'white';
							timeCell.style.border = '.1em solid #ddd';
							timeCell.style.padding = '.25em';
							timeCell.textContent = `${hour} - ${hour + 1}`;
							row.appendChild(timeCell);

							// Add cells for each day
							for (let day = 1; day <= 5; day++) {
								// Find a matching class for this professor, day, and time slot
								const classData = filteredData.find(entry =>
									(type === 'Professor' ? entry.professor === name : entry.course === name) &&
									entry.day === ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'][day - 1] &&
									entry.time.startsWith(`${hour}:00`)
								);


								// Create the cell only if classData exists
								if (classData) {
									const cell = document.createElement('td');
									cell.style.border = '.1em solid black';
									cell.style.padding = '0.25em';
									cell.style.textAlign = 'left';

									    // Hover effect styles
									cell.addEventListener('mouseover', () => {
										cell.style.backgroundColor = '#f0f0f0'; // Light gray background
										cell.style.color = '#333'; // Darker text color

										// Display a tooltip with classData.time
										const tooltip = document.createElement('div');
										tooltip.textContent = `Time: ${classData.time}`;
										tooltip.style.position = 'absolute';
										tooltip.style.top = '-1.5em'; // Position above the cell
										tooltip.style.left = '50%';
										tooltip.style.transform = 'translateX(-50%)';
										tooltip.style.padding = '0.25em 0.5em';
										tooltip.style.backgroundColor = '#000'; // Black background
										tooltip.style.color = '#fff'; // White text
										tooltip.style.borderRadius = '0.25em';
										tooltip.style.fontSize = '12px';
										tooltip.style.zIndex = '1000';
										tooltip.style.whiteSpace = 'nowrap'; // Prevent wrapping
										tooltip.style.boxShadow = '0 2px 4px rgba(0, 0, 0, 0.2)'; // Add shadow for better visibility
										tooltip.setAttribute('id', 'tooltip'); // Add an ID to find and remove it later

										// Ensure the parent container has 'relative' position for proper tooltip positioning
										cell.style.position = 'relative';

										cell.appendChild(tooltip);
									});

									cell.addEventListener('mouseout', () => {
										cell.style.backgroundColor = ''; // Reset background color
										cell.style.color = ''; // Reset text color

										// Remove the tooltip
										const tooltip = cell.querySelector('#tooltip');
										if (tooltip) {
											cell.removeChild(tooltip);
										}
									});


									// Extract startHour and endHour from time
									const [startHour, endHour] = classData.time.split(' - ').map(hour => parseInt(hour.split(':')[0], 10));
									const duration = endHour - startHour; // Calculate duration (end - start)

									// Set rowspan based on the duration
									const rowspan = duration > 1 ? duration : 1;
									cell.setAttribute('rowspan', rowspan);



									// Helper function to create a label-value row
									function createLabelValueRow(labelText, valueText) {
										const container = document.createElement('div');
										container.style.display = 'flex';
										container.style.justifyContent = 'space-between';
										container.style.marginBottom = '0.5em';

										const label = document.createElement('span');
										label.textContent = labelText;
										label.style.fontWeight = 'bold'; // Bold for the label

										const value = document.createElement('span');
										value.textContent = valueText;

										container.appendChild(label);
										container.appendChild(value);

										return container;
									}
									
									// Populate cell content based on type
									if (type === 'Professor') {
									    // Create rows for each attribute
										const courseRow = createLabelValueRow('Course:', classData.course);
										const roomRow = createLabelValueRow('Room:', classData.room);
										const groupRow = createLabelValueRow('Group:', classData.group);
										const labRow = createLabelValueRow('Lab:', classData.lab);

										// Append rows to the cell
										cell.appendChild(courseRow);
										cell.appendChild(roomRow);
										cell.appendChild(groupRow);
										cell.appendChild(labRow);

									} else if (type === 'Course') {
										const professorRow = createLabelValueRow('Professor:', classData.professor);
										const roomRow = createLabelValueRow('Room:', classData.room);
										const groupRow = createLabelValueRow('Group:', classData.group);
										const labRow = createLabelValueRow('Lab:', classData.lab);

										// Append rows to the cell
										cell.appendChild(professorRow);
										cell.appendChild(roomRow);
										cell.appendChild(groupRow);
										cell.appendChild(labRow);
									}

									row.appendChild(cell);
									
								} else {
									// If no classData is found, append an empty cell to maintain structure
									const emptyCell = document.createElement('td');
									row.appendChild(emptyCell);
								}
							}
							table.appendChild(row);
						}

						// Append the table to the result section
						resultSection.appendChild(table);

					}

					document.getElementById('searchBtn').addEventListener('click', performSearch);
					document.getElementById('downloadBtn').addEventListener('click', downloadPDF);

				</script>				
            ");

			var slotTable = new Dictionary<Point, int[]>();
			var timeTable = GenerateTimeTable(solution, slotTable); // Point.X = time, Point.Y = roomId

			if (slotTable.Count == 0 || timeTable.Count == 0)
				return "";
			
			
			// Table generation logic remains the same
			// Start wrapping rooms in pages
			int divCounter = 0;
			for (int roomId = 0; roomId < nr; ++roomId)
			{
				var room = solution.Configuration.GetRoomById(roomId);

				// Start a new page wrapper every two rooms
				if (divCounter % 2 == 0)
				{
					if (divCounter > 0)
					{
						sb.Append("</div>"); // Close the previous page
					}
					sb.Append("<div style='page-break-after: always;'>"); // Start a new page
				}				

				for (int periodId = 0; periodId < ROOM_ROW_NUMBER; ++periodId)
				{
					if (periodId == 0)
					{
						sb.Append("<div id='room_").Append(room.Name).Append("' style='padding: 0.5em'>\n");
						sb.Append("<table style='border-collapse: collapse; width: 95%'>\n");
						sb.Append(GetTableHeader(room));
					}
					else
					{						
						var key = new Point(periodId, roomId);							
						var roomDuration = slotTable.ContainsKey(key) ? slotTable[key] : null;
						var roomSchedule = timeTable.ContainsKey(key) ? timeTable[key] : null;
						
					
						sb.Append("<tr>");
						for (int i = 0; i < ROOM_COLUMN_NUMBER; ++i)
						{
							if(i == 0)
							{
								sb.Append("<th style='border: .1em solid black; padding: .25em' scope='row' colspan='2'>")
									.Append(PERIODS[periodId])
									.Append("</th>\n");
								continue;
							}

							if (roomSchedule == null && roomDuration == null)
								continue;

							string content = (roomSchedule != null) ? roomSchedule[i] : null;
							sb.Append(GetHtmlCell(content, roomDuration[i]));							
						}
						sb.Append("</tr>\n");							
					}

					if (periodId == ROOM_ROW_NUMBER - 1)
						sb.Append("</table>\n</div>\n");
				}

				divCounter++;

				if (divCounter % 2 != 0)
				{
					sb.Append("</div>");
				}
			}

			// Close the last page wrapper if necessary
			if (nr > 0 && nr % 2 != 0)
			{
				sb.Append("</div>");
			}


			
			return sb.ToString();
		}
	}
}
