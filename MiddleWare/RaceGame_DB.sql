CREATE TABLE races (
    race_id INT AUTO_INCREMENT PRIMARY KEY,
    start_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    winner_name VARCHAR(50) DEFAULT NULL,
    total_duration_seconds FLOAT DEFAULT 0.0
);

CREATE TABLE lap_times (
    lap_id INT AUTO_INCREMENT PRIMARY KEY,
    race_id INT,
    car_name VARCHAR(50),
    lap_number INT,
    lap_time_seconds FLOAT,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (race_id) REFERENCES races(race_id) ON DELETE CASCADE
);

CREATE TABLE telemetry_history (
    telemetry_id INT AUTO_INCREMENT PRIMARY KEY,
    race_id INT,
    car_name VARCHAR(50),
    pixel_x INT,
    pixel_y INT,
    current_lap INT,
    timestamp_offset FLOAT, -- Seconds passed since the race started
    FOREIGN KEY (race_id) REFERENCES races(race_id) ON DELETE CASCADE
);
