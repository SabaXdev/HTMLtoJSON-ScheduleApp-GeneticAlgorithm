using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;

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

		// public static string GetResult(Schedule solution)
		// {
		// 	StringBuilder sb = new StringBuilder();
		// 	int nr = solution.Configuration.NumberOfRooms;

		// 	var slotTable = new Dictionary<Point, int[]>();
		// 	var timeTable = GenerateTimeTable(solution, slotTable); // Point.X = time, Point.Y = roomId
		// 	if (slotTable.Count == 0 || timeTable.Count == 0)
		// 		return "";

		// 	for (int roomId = 0; roomId < nr; ++roomId)
		// 	{
		// 		var room = solution.Configuration.GetRoomById(roomId);
		// 		for (int periodId = 0; periodId < ROOM_ROW_NUMBER; ++periodId)
		// 		{
		// 			if (periodId == 0)
		// 			{
		// 				sb.Append("<div id='room_").Append(room.Name).Append("' style='padding: 0.5em'>\n");
		// 				sb.Append("<table style='border-collapse: collapse; width: 95%'>\n");
		// 				sb.Append(GetTableHeader(room));
		// 			}
		// 			else
		// 			{						
		// 				var key = new Point(periodId, roomId);							
		// 				var roomDuration = slotTable.ContainsKey(key) ? slotTable[key] : null;
		// 				var roomSchedule = timeTable.ContainsKey(key) ? timeTable[key] : null;
		// 				sb.Append("<tr>");
		// 				for (int i = 0; i < ROOM_COLUMN_NUMBER; ++i)
		// 				{
		// 					if(i == 0)
		// 					{
		// 						sb.Append("<th style='border: .1em solid black; padding: .25em' scope='row' colspan='2'>").Append(PERIODS[periodId]).Append("</th>\n");
		// 						continue;
		// 					}

		// 					if (roomSchedule == null && roomDuration == null)
		// 						continue;

		// 					string content = (roomSchedule != null) ? roomSchedule[i] : null;
		// 					sb.Append(GetHtmlCell(content, roomDuration[i]));							
		// 				}
		// 				sb.Append("</tr>\n");							
		// 			}

		// 			if (periodId == ROOM_ROW_NUMBER - 1)
		// 				sb.Append("</table>\n</div>\n");
		// 		}
		// 	}

		// 	return sb.ToString();
		// }
		
		public static string GetResult(Schedule solution)
		{
			StringBuilder sb = new StringBuilder();
			int nr = solution.Configuration.NumberOfRooms;
			// Add JavaScript to handle the search functionality
            // sb.Append("<script>");
            sb.Append(@"
			    <div class='buttons' style='margin-bottom: 1em; display: flex; align-items: center;'>
            	<label for='searchBy'>Search By: </label>
            	<select id='searchBy' name='searchBy' style='padding: 0.5em; margin-right: 1em;'>
            		<option value='professor'>Professor</option>
            		<option value='group'>Group</option>
            		<option value='course'>Course</option>
            	</select>
            		<input type='text' id='searchTerm' placeholder='Enter search term' style='padding: 0.5em; margin-right: 1em;'>
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


					function performSearch() {
						var searchBy = document.getElementById('searchBy').value;
						var searchTerm = document.getElementById('searchTerm').value;
					    const cells = document.querySelectorAll('td:not(:empty)');

						// Reset previous highlights
						cells.forEach(cell => {
							cell.style.border = '.1em solid black';
							cell.style.borderRadius = '';
							cell.style.boxShadow = '';
							cell.style.outline = '';
							cell.style.backgroundColor = '';
							cell.style.color = '';
							cell.style.fontWeight = ''; 
						});
		
						let matchFound = false;
						cells.forEach(cell => {
							// Extract text without HTML tags (like <span>, <br>, etc.)
							const cleanedText = cell.innerHTML
								.replace(/<br\s*\/?>/g, '\n') // Replace <br /> with a newline
								.replace(/<[^>]*>/g, '')      // Remove other HTML tags
								.trim();                      // Trim leading/trailing whitespace

							// Split the content by newlines or spaces to separate each part
							const cellParts = cleanedText.split(/\s+/).filter(Boolean);

							// Determine which index to check based on `searchBy`
							let indexToCheck = -1; // Default to -1 (invalid index)
							if (searchBy === 'course') {
								indexToCheck = 0;
							} else if (searchBy === 'professor') {
								indexToCheck = 1;
							} else if (searchBy === 'group') {
								indexToCheck = 2;
							}

							if (indexToCheck >= 0 && cellParts[indexToCheck]?.toLowerCase() === searchTerm.trim().toLowerCase()) {
								cell.style.outline = '0.1em solid #8A2BE2';                  // Solid purple
								cell.style.borderRadius = '5px';                             // Slightly rounded
								cell.style.backgroundColor = '#2C2F33';                      // Dark gray background
								cell.style.color = '#FFFFFF';                                // White text
								cell.style.boxShadow = '0 4px 8px rgba(0, 0, 0, 0.2)';       // Subtle shadow
								cell.style.fontWeight = 'bold';                              // Bold text for emphasis
								matchFound = true;
							}
						});
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
