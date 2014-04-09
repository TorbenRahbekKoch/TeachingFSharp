﻿namespace ColorPuzzle
module GameBoard = 

    type TileColor = TileColor of int
    with member x.value() = 
            match x with
            | TileColor(n) -> n

    type Position = {
        x : int
        y : int
    }

    type Tile = {
        color    : TileColor
        position : Position
    }

    let randomGenerator = System.Random()

    let getRandom n =
        randomGenerator.Next n

    let createRandomColor numberOfColors =
        let colorIndex = getRandom numberOfColors
        TileColor colorIndex
            
    type GameBoard = {
        sizeX         : int
        sizeY         : int  
        numberOfColors: int
        currentColor  : TileColor
        currentTiles  : Set<Tile>
        tiles         : Tile array array
        numberOfMoves : int
        score         : int  
        isFinished    : bool
    }

    let getNeighborTiles sizeX sizeY tile (tiles: Tile array array) =
        let point = (tile.position.x, tile.position.y)
        let leftNeighbor =
            match point with
            | (x, y) when x > 0 -> Some(tiles.[y].[x-1])
            | _ -> None

        let rightNeighbor =
            match point with
            | (x, y) when x < sizeX-1 -> Some(tiles.[y].[x+1])
            | _ -> None
             
        let topNeighbor =
            match point with
            | (x, y) when y > 0 -> Some(tiles.[y-1].[x])
            | _ -> None
             
        let bottomNeighbor =
            match point with
            | (x, y) when y < sizeY-1 -> Some(tiles.[y+1].[x])
            | _ -> None             

        let neighborTiles = [leftNeighbor]
                             @[rightNeighbor]
                             @[topNeighbor]
                             @[bottomNeighbor]
                             |> List.choose id
                             |> List.map(fun tile -> tiles.[tile.position.y].[tile.position.x])
        neighborTiles
                             

    let calculateCurrentTileSet sizeX sizeY (tiles : Tile array array) : Set<Tile> = 
        let rec findSameColoredNeighbors currentTile (alreadyFound: Set<Tile>) : Set<Tile> =
            let neighborTiles = getNeighborTiles sizeX sizeY currentTile  tiles
                                |> List.filter (fun tile -> tile.color = currentTile.color)
                                |> List.filter (fun item -> not (Set.contains item alreadyFound))

            let mutable nowFound = Set.union (Set.ofList neighborTiles) alreadyFound

            for tile in neighborTiles do
                let sameColorNeighbors = findSameColoredNeighbors tile nowFound
                nowFound <- Set.union sameColorNeighbors nowFound

            nowFound

        let rootTile = tiles.[0].[0]
        findSameColoredNeighbors rootTile (Set [rootTile]) 

    let createTiles sizeX sizeY numberOfColors =
        [| for y in 0..sizeY-1 -> 
            [|for x in 0..sizeX-1 -> 
                {color = createRandomColor numberOfColors; position = {x = x; y = y} }|]  |] 

    let createGameBoard sizeX sizeY numberOfColors =
        let tiles = createTiles sizeX sizeY numberOfColors
        { 
            currentColor = tiles.[0].[0].color
            sizeX        = sizeX
            sizeY        = sizeY 
            numberOfColors = numberOfColors
            tiles        = tiles
            currentTiles = calculateCurrentTileSet sizeX sizeY tiles
            numberOfMoves = 0
            score         = 0
            isFinished    = false
        }
            
    let moveBoardToNextLevel gameBoard = 
        if not gameBoard.isFinished then
            gameBoard
        else
            let newGameBoard = createGameBoard (gameBoard.sizeX+1) (gameBoard.sizeY+1) gameBoard.numberOfColors
            { newGameBoard with
                score = gameBoard.score
            }

    let paintBoardWithColor gameBoard newColor =
        if gameBoard.isFinished then
            gameBoard
        elif gameBoard.currentColor = newColor then
            gameBoard
        else
            let sizeX = gameBoard.sizeX
            let sizeY = gameBoard.sizeY

            let scoreDiff = sizeX * sizeY - gameBoard.numberOfMoves

            let tiles = [| for y in 0..sizeY-1 ->
                            [|for x in 0..sizeX-1 ->
                               let tile = gameBoard.tiles.[y].[x]
                               if Set.contains tile gameBoard.currentTiles then
                                   { tile with color = newColor }
                               else
                                   tile
                            |]
                        |]         

            let newCurrentTiles = calculateCurrentTileSet sizeX sizeY tiles
            let isFinished = newCurrentTiles.Count = gameBoard.sizeX * gameBoard.sizeY

            if isFinished then
                { gameBoard with 
                    score = gameBoard.score + scoreDiff
                    tiles = tiles
                    isFinished = true
                    numberOfMoves = gameBoard.numberOfMoves + 1
                 }
            else            
                if newCurrentTiles.Count = gameBoard.currentTiles.Count then
                    gameBoard
                else          
                    let oldTopRightTile = gameBoard.tiles.[0].[sizeX-1]
                    let oldBottomRightTile = gameBoard.tiles.[sizeY-1].[sizeX-1]
                    let oldBottomLeftTile = gameBoard.tiles.[sizeY-1].[0]
                    let newTopRightTile = tiles.[0].[sizeX-1]
                    let newBottomRightTile = tiles.[sizeY-1].[sizeX-1]
                    let newBottomLeftTile = tiles.[sizeY-1].[0]

                    let hasTile newTile oldTile =
                        (Set.contains newTile newCurrentTiles) && not (Set.contains oldTile gameBoard.currentTiles)

                    let newScore = gameBoard.score  
                                 + (if hasTile newTopRightTile oldTopRightTile then scoreDiff else 0)
                                 + (if hasTile newBottomRightTile oldBottomRightTile then scoreDiff else 0)
                                 + (if hasTile newBottomLeftTile oldBottomLeftTile then scoreDiff else 0)

                    { gameBoard with 
                        currentColor = newColor
                        score = newScore
                        tiles = tiles 
                        currentTiles = newCurrentTiles
                        numberOfMoves = gameBoard.numberOfMoves + 1
                     }

    module Persistence =

        let tileToList tile =
            [tile.color.value()]
            @[tile.position.x]
            @[tile.position.y]

        let tileRowToList tileRow : int list =
            tileRow 
            |> List.collect(tileToList)

        let tilesToList tiles : int list =
            tiles 
            |> Array.toList
            |> List.collect(fun row -> row |> Array.toList |> tileRowToList)


        let saveGameBoard gameBoard =
            Array.empty<int>

        let loadTile (saved: int[]) position =
            { 
                color = TileColor(saved.[position + 0])
                position = { x = saved.[position + 1]; y = saved.[position + 2]}
            }

        let loadGameBoard (saved: int[]) =
            {
                sizeX = 0
                sizeY = 0
                numberOfColors = 0
                currentColor = TileColor 0
                numberOfMoves = 0
                score = 0
                isFinished = false
                tiles = Array.empty
                currentTiles = Set []
            }

