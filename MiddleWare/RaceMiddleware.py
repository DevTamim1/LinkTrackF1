import cv2
import cv2.aruco as aruco
import serial
import time
import socket
import json
import mysql.connector

SERIAL_PORT = 'COM7'  
BAUD_RATE = 9600
UNITY_IP = "127.0.0.1"
UNITY_PORT = 5005       

DB_CONFIG = {
    "host": "localhost",
    "user": "root",             
    "password": "1234", 
    "database": "RaceGame"
}

CAMERA_INDEX = 1 
ARUCO_DICT = aruco.getPredefinedDictionary(aruco.DICT_4X4_50)
ARUCO_PARAMETERS = aruco.DetectorParameters()
ARUCO_PARAMETERS.adaptiveThreshWinSizeMin = 3
ARUCO_PARAMETERS.adaptiveThreshWinSizeStep = 10

ARUCO_TO_CAR = {
    0: "Red #43",
    1: "Green #18",
    2: "Orange #61",
    3: "Yellow Car",
    4: "Black #44",
    5: "Red #76",
    6: "Blue #9",
    7: "Green #66"
}

car_state = {name: {"lap_number": 0, "last_lap_time": 0.0, "start_time": None} for name in ARUCO_TO_CAR.values()}

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) 
cap = cv2.VideoCapture(CAMERA_INDEX, cv2.CAP_DSHOW)

print("====== 🏁 DAY 2 HYBRID RACE ENGINE ONLINE 🏁 ======")
print(f"Broadcasting Live JSON Telemetry to Unity on port {UNITY_PORT}...")

try:
    db_conn = mysql.connector.connect(**DB_CONFIG)
    db_cursor = db_conn.cursor()
    
    db_cursor.execute("INSERT INTO races (winner_name) VALUES (NULL)")
    db_conn.commit()
    CURRENT_RACE_ID = db_cursor.lastrowid
    RACE_START_WALL_TIME = time.time()
    print(f"MySQL Connected. Storing data under SESSION RACE ID: {CURRENT_RACE_ID}")
except Exception as e:
    print(f"Database connection error: {e}")
    print("Please check your MySQL configuration and try again.")
    exit()

try:
    ser = serial.Serial(SERIAL_PORT, BAUD_RATE, timeout=0.1)
    time.sleep(2) 
    print("Arduino Connected.")
except Exception as e:
    print(f"Arduino not found on {SERIAL_PORT}. Running Camera-Only mode.")
    ser = None

try:
    while True:
        if ser and ser.in_waiting > 0:
            raw_line = ser.readline().decode('utf-8', errors='ignore').strip()
            
            if raw_line.startswith("LAP_TRIGGER:"):
                current_time = time.time()  
                car_name = raw_line.split(":")[1].strip()
                
                if car_name in car_state:
                    state = car_state[car_name]
                    state["lap_number"] += 1
                    
                    if state["start_time"] is None:
                        print(f"🚦 {car_name} crossed the start line!")
                        state["last_lap_time"] = 0.0
                    else:
                        lap_time = current_time - state["start_time"]
                        state["last_lap_time"] = round(lap_time, 3)
                        print(f"🏁 {car_name} finished Lap {state['lap_number'] - 1} in {state['last_lap_time']}s")
                        
                        # 💾 LOG LAP TO MYSQL
                        sql = "INSERT INTO lap_times (race_id, car_name, lap_number, lap_time_seconds) VALUES (%s, %s, %s, %s)"
                        db_cursor.execute(sql, (CURRENT_RACE_ID, car_name, state["lap_number"] - 1, state["last_lap_time"]))
                        db_conn.commit()
                    
                    state["start_time"] = current_time

        ret, frame = cap.read()
        if not ret:
            continue

        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        corners, ids, _ = aruco.detectMarkers(gray, ARUCO_DICT, parameters=ARUCO_PARAMETERS)
        
        if ids is not None:
            time_offset = round(time.time() - RACE_START_WALL_TIME, 3)
            
            for i in range(len(ids)):
                marker_id = ids[i][0]
                
                if marker_id in ARUCO_TO_CAR:
                    car_name = ARUCO_TO_CAR[marker_id]
                    c = corners[i][0]
                    
                    center_x = int((c[0][0] + c[2][0]) / 2)
                    center_y = int((c[0][1] + c[2][1]) / 2)
                    
                    telemetry_sql = """INSERT INTO telemetry_history 
                                       (race_id, car_name, pixel_x, pixel_y, current_lap, timestamp_offset) 
                                       VALUES (%s, %s, %s, %s, %s, %s)"""
                    db_cursor.execute(telemetry_sql, (CURRENT_RACE_ID, car_name, center_x, center_y, car_state[car_name]["lap_number"], time_offset))
                    
                    telemetry_packet = {
                        "car_name": car_name,
                        "x": center_x,
                        "y": center_y,
                        "lap": car_state[car_name]["lap_number"],
                        "lap_time": car_state[car_name]["last_lap_time"]
                    }
                    
                    json_string = json.dumps(telemetry_packet)
                    sock.sendto(json_string.encode('utf-8'), (UNITY_IP, UNITY_PORT))
                    
                    cv2.circle(frame, (center_x, center_y), 5, (0, 255, 0), -1)
                    cv2.putText(frame, car_name, (center_x + 10, center_y), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)

            db_conn.commit()

        cv2.imshow("Live Track Feed", frame)
        
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

except KeyboardInterrupt:
    print("\nShutting down Race Engine...")
finally:
    if 'CURRENT_RACE_ID' in locals():
        total_duration = round(time.time() - RACE_START_WALL_TIME, 2)
        db_cursor.execute("UPDATE races SET total_duration_seconds = %s WHERE race_id = %s", (total_duration, CURRENT_RACE_ID))
        db_conn.commit()
        print(f"Race session {CURRENT_RACE_ID} finalized successfully inside DB.")
        
    if ser:
        ser.close()
    db_cursor.close()
    db_conn.close()
    cap.release()
    cv2.destroyAllWindows()