cmake_minimum_required(VERSION 3.20)

project(SimulationCore C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

add_library(SimulationCore SHARED
    src/simulation_core.c
)

target_include_directories(SimulationCore
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}/include
)

if(WIN32)
    target_compile_definitions(SimulationCore PRIVATE SIMULATIONCORE_EXPORTS)
endif()

set_target_properties(SimulationCore PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/../UnityProject/Assets/Plugins/x86_64"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/../UnityProject/Assets/Plugins/x86_64"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/build/lib"
)
