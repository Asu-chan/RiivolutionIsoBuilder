# Asu's Riivolution ISO Builder
A tool to patch Nintendo Wii ISO files using Riivolution XML files.

# Usage
RiivolutionIsoBuilder.exe \<ISO Path\> \<Riivolution XML file path\> \<Output ISO/WBFS path\> [options]

RiivolutionIsoBuilder.exe [options]

RiivolutionIsoBuilder.exe

(Note: In the 2nd and 3rd cases, you will be asked for the file paths.)

# Options
--silent                  -\> Prevents from displaying any console outputs apart from the necessary ones

--always-single-choice    -\> Enables by default any option that has only one choice

--never-single-choice     -\> Disable by default any option that has only one choice

--title-id \<TitleID\>    -\> Changes the TitleID of the output rom; Replace with dots the characters that should be kept

--game-name \<Game name\> -\> Changes the TitleID of the output rom; Replace with dots the characters that should be kept

--keep-extracted-iso      -\> Prevents the extractedISO folder from being deleted after the end of the process

--ignore-warnings         -\> If the builder hits any warning, it'll be ignored and building will proceed [USE IF YOU KNOW WHAT YOU ARE DOING]

--ignore-errors           -\> If the builder hits any error or warning, it'll be ignored and building will proceed [USE IF YOU KNOW WHAT YOU ARE DOING]
