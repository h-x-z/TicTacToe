using Godot;
using System;

public class Main : Node2D
{
	// Used to determine which slot is selected by the user
	// and show the respective sprites
	private int selectedSlot = 1;
	// When this is set to true the player won't be able to select
	// any slots, this is used when a round has concluded
	// or when the bot is selecting its slot
	private bool GamePause = false;
	// A 2 second timer before clearing the board after a game
	// has concluded
	Godot.Timer cbTimer;
	// The clear board function uses this to check which conclusion
	// message to hide, the possible conditions are "Win", "Loss"
	// and "Draw"
	private string condition = null;
	
	// Used to determine which spots have been taken and which
	// spots are available to plot
	private string[] board = {
		"", "", "",
		"", "", "",
		"", "", ""
	}; 
	
	// List of all possible win patterns, each number represents
	// a slot on the board
	private int[,] WinPatterns = {
		{1, 4, 7},
		{2, 5, 8},
		{3, 6, 9},
		{1, 2, 3},
		{4, 5, 6},
		{7, 8, 9},
		{1, 5, 9},
		{7, 5, 3}
	};

	///<summary> This function runs once at the beginning of the game</summary>
	public override void _Ready()
	{
		// Initiating timer
		cbTimer = GetNode<Godot.Timer>("Timers/ClearBoardTimer");
		cbTimer.Connect("timeout", this, nameof(ClearBoardAndTimeout));
		// Making the selection sprite visible on the first slot
		UpdateSelection();
	}
	
	///<summary> This function is executed every frame</summary>
	public override void _Process(float delta)
	{
		// Adding a cross when enter or space is pressed
		// and the game is not paused (round already concluded)
		if (Input.IsActionJustReleased("ui_accept") && !GamePause) {
			// Checks to see if the selected slot has already been taken
			if (board[selectedSlot - 1] == "") {
				// Setting the slot to a cross
				board[selectedSlot - 1] = "X";
				GetNode<CanvasItem>($"Crosses/Cross_{selectedSlot}").Visible = true;
				
				// Checking if the player has won or if the game is
				// a draw (board is full but no winners)
				int won = CheckWin("Crosses", "Cross");
				if (won != -1) {
					// Increments score
					AddScore("Player");
					
					// Makes a green streak visible over the pattern which won
					GetNode<CanvasItem>($"Streaks/Streak_{(won + 1)}").Visible = true;
					
					// Displays a win message then initiates the
					// clearboard timer and pauses the game
					condition = "Win";
					// Plays sound effect
					SFX("Conclude");
					cbTimer.Start();
					GetNode<CanvasItem>("Messages/Win").Visible = true;
					GamePause = true;
				} else if (CheckDraw()) {
					// Checks if there's a draw then displays a draw message
					// and initiates clearboard timer and pauses the game
					condition = "Draw";
					// Plays sound effect
					SFX("Conclude");
					cbTimer.Start();
					GetNode<CanvasItem>("Messages/Draw").Visible = true;
					GamePause = true;
				}
				
				// Checks if the game condition is null before making
				// the bot choose a slot, this is done to make sure the game
				// isn't concluded
				if (condition == null) {
					BotSelect();
					// Plays sound effect
					SFX("Select");
				}
			}
		}
		
		// If the right arrow is pressed then move the selection to the right
		// also checks if the current slot is less than 9
		// since the selection can not move past 9
		else if (Input.IsActionJustReleased("ui_right") && selectedSlot < 9) {
			selectedSlot++;
			// Plays sound effect
			SFX("MoveSelection");
			UpdateSelection();
		}
		
		// If the left arrow is pressed then move the selection to the left
		// also checks if the current slot is bigger than 1
		// since the selection can not move before 1
		else if (Input.IsActionJustReleased("ui_left") && selectedSlot > 1)
		{
			selectedSlot--;
			// Plays sound effect
			SFX("MoveSelection");
			UpdateSelection();
		}
		
		// If the down arrow is pressed then move the selection down
		// also checks if the current slot is under 7
		// meaning that the current selection is not on the last row
		// since the selection is unable to move further than the last row
		else if (Input.IsActionJustReleased("ui_down") && selectedSlot < 7)
		{
			selectedSlot += 3;
			// Plays sound effect
			SFX("MoveSelection");
			UpdateSelection();
		}
		
		// If the up arrow is pressed then move the selection up
		// also checks if the current slot is over 3
		// meaning that the current selection would already be at least on the
		// second row, as the selection can't move up if it's on the first row
		else if (Input.IsActionJustReleased("ui_up") && selectedSlot > 3)
		{
			selectedSlot -= 3;
			// Plays sound effect
			SFX("MoveSelection");
			UpdateSelection();
		} 
	}

	///<summary> The function checks whether the game is a draw this is done by checking if there are no slots remaining</summary>
	private bool CheckDraw() {
		// Returns whether it found an empty slot in the board
		return (Array.IndexOf(board, "") == -1);
	}

	///<summary> This function hides all selections sprites and makes the sprite for the current selection visible</summary>
	private void UpdateSelection() {
		// Loops over each slot sprite
		for (int i = 1; i < 10; i++) {
			// If it is corresponding sprite for the current selection it will
			// make it visible
			if (i == selectedSlot) {
				GetNode<CanvasItem>($"SelectionSlots/Slot_{selectedSlot}").Visible = true;
			} else {
				// Hides any other selection sprites
				GetNode<CanvasItem>($"SelectionSlots/Slot_{i}").Visible = false;
			}
		}
	}
	
	///<summary> This is used to clear a board after a game concludes</summary>
	private void ClearBoard() {
		// Loops over each slot in board
		for (int i = 1; i < 10; i++) {
			// Makes each slot in the board empty
			board[i - 1] = "";
			
			// Hides all streaks
			if ((i - 1) < 8) {
				GetNode<CanvasItem>($"Streaks/Streak_{i}").Visible = false;
			}
			
			// Hides each note and cross
			GetNode<CanvasItem>($"Crosses/Cross_{i}").Visible = false;
			GetNode<CanvasItem>($"Notes/Note_{i}").Visible = false;
		}
	}
	
	///<summary> Returns whether a cross/note sprite is visible</summary>
	private bool Selected(string node, string name) {
		return GetNode<CanvasItem>($"{node}/{name}").Visible;
	}
	
	///<summary> Checks whether notes/crosses have won, node refers to the group node (ie. Notes) and type refers to the sprite prefix (ie. Note - Note_1)</summary>
	private int CheckWin(string node, string type) {
		// List of patterns which the node/type matches
		bool[] Matches = new bool[8];
		
		// Loops over each win pattern
		for (int i = 0; i < 8; i++) {
			// Checks if the node/type is matching all 3 spots in the pattern
			Matches[i] = Selected(node, $"{type}_{WinPatterns[i, 0]}") &&
				Selected(node, $"{type}_{WinPatterns[i, 1]}") &&
				Selected(node, $"{type}_{WinPatterns[i, 2]}");
		}
		
		// Returning which pattern the node/type matched
		return Array.IndexOf(Matches, true);
	}

	///<summary> The code for determining which slot the bot will select</summary>
	private void BotSelect() {
		// Pauses the game so the player is unable to choose any slots during
		// this period
		GamePause = true;
		// This variable is used to determine whether a pattern has been found
		// where the player is almost about to win (the player has 2/3 slots
		// for the pattern)
		bool found = false;
		
		// Loops over each win pattern
		for (int i = 0; i < 8; i++) {
			// This will not run for the remaining loop iterations if a pattern
			// has been found which the player has almost completed
			if (!found) {
				
				// List of slots the player has selected from the
				// current pattern
				bool[] CStreaks = {
					Selected("Crosses", $"Cross_{WinPatterns[i, 0]}"),
					Selected("Crosses", $"Cross_{WinPatterns[i, 1]}"),
					Selected("Crosses", $"Cross_{WinPatterns[i, 2]}")
				};
				
				// List of slots the bot has selected from the
				// current pattern, used to determine whether the bot
				// should finish a winpattern and win the game or if
				// it had no winpatterns it can complete then blocks any 
				// win patterns the player might have almost completed
				bool[] NStreaks = {
					Selected("Notes", $"Note_{WinPatterns[i, 0]}"),
					Selected("Notes", $"Note_{WinPatterns[i, 1]}"),
					Selected("Notes", $"Note_{WinPatterns[i, 2]}")
				};
				
				// Checks if out of the pattern slots, the bot has selected
				// 2/3 and the third slot is empty
				if (Array.FindAll(NStreaks, s => s == false).Length == 1 && board[(WinPatterns[i, Array.IndexOf(NStreaks, false)] - 1)] == "") {
					// Increments score
					AddScore("Bot");
					
					// Takes the slot and makes the note visible there
					board[(WinPatterns[i, Array.IndexOf(NStreaks, false)] - 1)] = "O";
					GetNode<CanvasItem>($"Notes/Note_{(WinPatterns[i, Array.IndexOf(NStreaks, false)])}").Visible = true;
					
					// Sets found to true so the code is not run for the 
					// remaining iterations
					found = true;
					// A streak is displayed over the winpattern
					GetNode<CanvasItem>($"Streaks/Streak_{(CheckWin("Notes", "Note") + 1)}").Visible = true;
					// Game is concluded and condition is set to Loss (because
					// the player has lost)
					condition = "Loss";
					// Plays sound effect
					SFX("Conclude");
					// Timer is started to clear the board and a Loss message is
					// shown
					cbTimer.Start();
					GetNode<CanvasItem>("Messages/Loss").Visible = true;
				}
				// If the bot can not complete the win pattern then it
				// will check if the player can complete it
				else if (Array.FindAll(CStreaks, s => s == false).Length == 1 && board[(WinPatterns[i, Array.IndexOf(CStreaks, false)] - 1)] == "") {
					// The bot takes the 3rd slot of the pattern to prevent the
					// player from winning
					board[(WinPatterns[i, Array.IndexOf(CStreaks, false)] - 1)] = "O";
					GetNode<CanvasItem>($"Notes/Note_{(WinPatterns[i, Array.IndexOf(CStreaks, false)])}").Visible = true;
					// found is set to true again to prevent it from iterating
					found = true;
					
					// Bot checks to see if it has won the game or if the game
					// is a draw
					int won = CheckWin("Notes", "Note");
					// Won will return either an index to a winpattern if the
					// bot has won or it will return -1 if no pattern was found
					// if won did not return -1 (meaning it has found a
					// winpattern which the bot matches) it will conclude the 
					// game and the player will lose
					if (won != -1) {
						// Increments score
						AddScore("Bot");
						
						// Plays sound effect
						SFX("Conclude");
					
						// Displays streak sprite and concludes the game as a
						// loss
						GetNode<CanvasItem>($"Streaks/Streak_{(won + 1)}").Visible = true;
						condition = "Loss";
						
						// Starts clearboard timer and shows the Loss message
						cbTimer.Start();
						GetNode<CanvasItem>("Messages/Loss").Visible = true;
					
					// Checks if board is full (draw)
					} else if (CheckDraw()) {
						// Condition is set to draw
						condition = "Draw";
						
						// Plays sound effect
						SFX("Conclude");
						
						// Clearboard timer is initiated and draw message is
						// shown
						cbTimer.Start();
						GetNode<CanvasItem>("Messages/Draw").Visible = true;
					}
				}
			}
			
			// If the bot has not found any winpattern which is almost completed
			// then it will choose a random slot to plot a note
			// it checks whether i is 7 to make sure this is the final pattern
			// which was not found
			if (!found && i == 7) {
				// A random index is chosen from the board
				Random rand = new Random();  
				int index = rand.Next(board.Length);
				// This bool determines whether an empty spot has been found
				// and if the loop should stop
				bool foundRSpot = false;
				
				// Loops until it finds an empty spot on the board
				do {
					// If the slot is not empty it chooses a new random index
					if (board[index] != "") {
						index = rand.Next(board.Length);
					} else {
						// If the slot is empty then it plots a note there
						board[index] = "O";
						GetNode<CanvasItem>($"Notes/Note_{index + 1}").Visible = true;
						
						// FoundRSpot is set to true to end the loop
						foundRSpot = true;
						
						// Checks if bot has won the game
						int won = CheckWin("Notes", "Note");
						if (won != -1) {
							// Increments score
							AddScore("Bot");
							
							// Plays sound effect
							SFX("Conclude");
							
							// If won, places a streak and concludes the game
							// with a Loss (for the player)
							GetNode<CanvasItem>($"Streaks/Streak_{(won + 1)}").Visible = true;
							condition = "Loss";
							
							// The clearboard timer is started and a Loss
							// message is displayed
							cbTimer.Start();
							GetNode<CanvasItem>("Messages/Loss").Visible = true;
							
						// If the bot has not won it checks for a draw
						} else if (CheckDraw()) {
							// Condition is set to draw and the clearboard timer
							// is started then a draw message is displayed
							condition = "Draw";
							// Plays sound effect
							SFX("Conclude");
							cbTimer.Start();
							GetNode<CanvasItem>("Messages/Draw").Visible = true;
						}
					}
					
				// Runs until an empty random spot has been found
				} while (foundRSpot == false);
			}
		}
		
		// If the game has not concluded it will unpause the game
		// which will allow the player to choose a new slot
		if (condition == null) GamePause = false;
	}
	
	///<summary> Clears board and hides all messages</summary>
	private void ClearBoardAndTimeout() {
		// Clears all the notes and crosses
		ClearBoard();
		
		// Hides the condition message
		GetNode<CanvasItem>($"Messages/{condition}").Visible = false;
		// Stops the timer so the clearboard function isn't looped every
		// 2 seconds
		cbTimer.Stop();
		// Unpauses the game and sets the games condition back to null
		GamePause = false;
		condition = null;
	}
	
	///<summary> Increments score for a user</summary>
	private void AddScore(string user) {
		// User should either be "Player" or "Bot"
		// Parses current score of user as an int
		int currentScore = int.Parse(GetNode<Label>($"Scores/{user}").Text);
		
		// Increments current score and replaces the score text for user
		currentScore++;
		GetNode<Label>($"Scores/{user}").Text = currentScore.ToString();
	}
	
	///<summary> Plays sound effects</summary>
	private void SFX(string sound) {
		// Sound should be "MoveSelection", "Select" or "Conclude"
		
		// Plays the sound
		GetNode<AudioStreamPlayer>($"Sounds/{sound}").Playing = true;
	}
}
