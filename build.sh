#!/bin/bash

# Directory to monitor for changes
dir="./Source"
MOD=$(basename $PWD)

# Define the path to your solution file
solutionPath="Source/${MOD}.sln"

# Define an array of configurations
configurations=("v1.2" "v1.3" "v1.4" "v1.5" "v1.6")

dotnet restore "$solutionPath"

function sync_mod() {
    # Copy over the mod directory.
    rsync -a ${MOD} /rimworld/1.2/Mods/

    # Copy over and reformat the README.
    cp README.md /rimworld/1.2/Mods/${MOD}
    unix2dos /rimworld/1.2/Mods/${MOD}/README.md

    rm -rf /rimworld/1.3/Mods/${MOD}
    rm -rf /rimworld/1.4/Mods/${MOD}
    rm -rf /rimworld/1.5/Mods/${MOD}
    rm -rf /rimworld/1.6/Mods/${MOD}
    rm -rf /rimworld/1.6-steam/Mods/${MOD}

    cp -af /rimworld/1.2/Mods/${MOD} /rimworld/1.3/Mods
    cp -af /rimworld/1.2/Mods/${MOD} /rimworld/1.4/Mods
    cp -af /rimworld/1.2/Mods/${MOD} /rimworld/1.5/Mods
    cp -af /rimworld/1.2/Mods/${MOD} /rimworld/1.6/Mods
    cp -af /rimworld/1.2/Mods/${MOD} /rimworld/1.6-steam/Mods
}

function build() {
    rm -rf /rimworld/1.2/Mods/${MOD}

    # Loop through each configuration and build it
    local pids=()
    for config in "${configurations[@]}"; do
        echo "Building for configuration: $config"
        dotnet build --no-restore "$solutionPath" --configuration "Release $config" &
        pids+=($!)
    done

    # Wait for each build individually so failures propagate.
    local failed=0
    for pid in "${pids[@]}"; do
        if ! wait "$pid"; then
            echo "Build failed for a configuration (pid $pid)." >&2
            failed=1
        fi
    done

    if [ "$failed" -ne 0 ]; then
        echo "Aborting sync: one or more configurations failed to build." >&2
        exit 1
    fi

    sync_mod

    echo "All builds completed!"
}

build
