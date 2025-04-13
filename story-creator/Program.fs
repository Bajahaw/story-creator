﻿open System

// story type 
type Genre = | Fantasy | Mystery | SciFi

type Stage =
    | Introduction       // set scene, introduce characters
    | ActionConflict     // main challenge, rising tension
    | AmbiguityMystery   // unpredictable paths, hints, twists
    | ClimaxResolution   // peak conflict and conclusion
    | End                // story finished

// state for the story
type StoryState = {
    Genre: Genre
    CurrentStage: Stage
    History: string list 
    CurrentText: string  
    Choices: string list 
}

module StoryEngine =
    
    let getNextStage current =
        match current with
        | Introduction -> ActionConflict
        | ActionConflict -> AmbiguityMystery
        | AmbiguityMystery -> ClimaxResolution
        | ClimaxResolution -> End
        | End -> End

    // (mock implementation)
    let callMockAiApi (prompt: string) : string * string list =
        printfn "\n--- AI Prompt (Debug) ---"
        printfn "%s" prompt
        printfn "--- End AI Prompt ---\n"

        let segment, choices =
            if prompt.Contains("Introduce") then
                "You stand at a misty crossroads in the ancient forest. A weathered signpost points in two directions.", ["Examine the 'Whispering Path' sign"; "Check the 'Silent Cave' sign"]
            elif prompt.Contains("ActionConflict") then
                let choice = if Random().Next(2) = 0 then "shadowy figure" else "sudden tremor"
                sprintf "As you proceed, a %s emerges! Danger is imminent." choice, ["Confront the threat directly"; "Look for cover or an escape route"]
            elif prompt.Contains("AmbiguityMystery") then
                "You discover a cryptic locket half-buried in the mud. It feels strangely warm.", ["Open the locket immediately"; "Leave the locket, sensing a trap"]
            elif prompt.Contains("ClimaxResolution") then
                "The final challenge stands before you! With a surge of effort, you overcome it. The immediate danger has passed.", ["Survey the aftermath"; "Begin your journey home"]
            elif prompt.Contains("End") then
                "And so, your adventure concludes. The echoes of your choices linger in the air.", [] // No choices at the end
            else // fallback
                "An unexpected calm settles. What happens next is unclear.", ["Wait and observe"; "Press onward cautiously"]

        Threading.Thread.Sleep(500)
        segment, choices

    let generatePrompt (state: StoryState) (userChoice: string option) : string =
        let stageGoal =
             match state.CurrentStage with
             | ActionConflict -> "Focus on rising action and conflict."
             | AmbiguityMystery -> "Introduce an element of ambiguity or mystery."
             | ClimaxResolution -> "Build towards the climax and resolution."
             | End -> "Provide a concluding paragraph and resolution. Offer no choices."
             | Introduction -> "Start the story with an introduction."

        let context =
            match state.History |> List.tryLast with
            | Some _ -> sprintf "Previous Event: %s" (String.concat " " state.History)
            | None -> "This is the beginning of the story."

        let choiceInfo =
            userChoice
            |> Option.map (sprintf "User chose: %s")
            |> Option.defaultValue ""

        sprintf "Generate the next part of a %A story. \n%s \n%s \nCurrent Narrative Goal: %s \nKeep the story segment concise (1-2 sentences). \nProvide 2 short, distinct choices for the user based on the segment. \n--- \n" state.Genre context choiceInfo stageGoal

    // story engine core - pure transition function (conceptually)
    let transition (currentState: StoryState) (userChoice: string) : StoryState =
        if currentState.CurrentStage = End then currentState 
        else
            let prompt = generatePrompt {currentState with CurrentStage = getNextStage currentState.CurrentStage} (Some userChoice)
            let nextText, nextChoices = callAiApi prompt
            let nextStage = getNextStage currentState.CurrentStage
            {
                Genre = currentState.Genre
                CurrentStage = nextStage
                History = currentState.History @ [nextText] 
                CurrentText = nextText
                Choices = if nextStage = End then [] else nextChoices 
            }

    let initializeStory (genre: Genre) : StoryState =
        let initialState = {
            Genre = genre
            CurrentStage = Introduction
            History = []
            CurrentText = "" // will be filled by AI
            Choices = []
        }
        let initialPrompt = generatePrompt initialState None
        let introText, introChoices = callAiApi initialPrompt
        { initialState with
            CurrentText = introText
            Choices = introChoices
            History = [introText] // start history with the intro
        }

module ConsoleUI =

    let displayState (state: StoryState) : unit =
        printfn "\n========================================"
        printfn "%s" state.CurrentText
        printfn "========================================"

        if not (List.isEmpty state.Choices) then
            printfn "What do you do next?"
            state.Choices
            |> List.iteri (fun i choice -> printfn "%d. %s" (i + 1) choice)
        else
            printfn "\n--- THE END ---"

    let rec getUserChoice (choices: string list) : string =
        if List.isEmpty choices then
             failwith "Cannot get user choice when no choices are available." 

        printf "> "
        match Console.ReadLine() |> Int32.TryParse with
        | true, index when index >= 1 && index <= List.length choices ->
            choices.[index - 1] 
        | _ ->
            printfn "Invalid input. Please enter a number between 1 and %d." (List.length choices)
            getUserChoice choices // Retry

    let rec selectGenre () : Genre =
        printfn "Choose your adventure's genre:"
        printfn "1. Fantasy"
        printfn "2. Mystery"
        printfn "3. SciFi"
        printf "> "
        match Console.ReadLine() with
        | "1" -> Fantasy
        | "2" -> Mystery
        | "3" -> SciFi
        | _ ->
            printfn "Invalid selection. Please enter 1, 2, or 3."
            selectGenre() // Retry

[<EntryPoint>]
let main argv =
    printfn "Welcome to the F# Interactive Story Generator!"
    let selectedGenre = ConsoleUI.selectGenre()

    let mutable currentState = StoryEngine.initializeStory selectedGenre

    while currentState.CurrentStage <> Stage.End do
        ConsoleUI.displayState currentState
        let userChoice = ConsoleUI.getUserChoice currentState.Choices
        currentState <- StoryEngine.transition currentState userChoice

    ConsoleUI.displayState currentState

    0 // exit code