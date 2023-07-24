#!/bin/bash

# Set the base name for the renamed files
basename="trace"

# Change to the directory that contains the files
#cd /path/to/directory

# Find all JSON files and sort them by modification time
files=($(find . -maxdepth 1 -type f -name "*.json" -printf "%T+\t%p\n" | sort | cut -f 2))

# Ask for confirmation before renaming files
echo "This would overwrite ${#files[@]} file(s). Proceed? [y/n]"
read response
if [[ "$response" != "y" ]]; then
    exit
fi

# Loop through the files and rename them
for i in "${!files[@]}"; do
    newname="${basename}-$(printf '%03d' $((i))).json"
    mv "${files[$i]}" "${newname}"
done