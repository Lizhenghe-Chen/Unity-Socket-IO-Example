import socketio
import eventlet
import random

# Create a Socket.IO server
socketio_server = socketio.Server(cors_allowed_origins="*")


# Handle client connection
@socketio_server.event
def connect(sid, environ):
    token = parse_query(environ.get("QUERY_STRING", "")).get("token")
    if token != "UNITY":
        raise ConnectionRefusedError(
            f"Authentication error: token [{token}] expected [UNITY]"
        )
    print(f"Client connected: {sid}")
    emit_message(sid, "hello", {"message": "Hello Unity! You are now in the server!"})


# Handle mission request
@socketio_server.on("mission_request")
def on_mission(sid, data):
    print(f"Mission request from {sid} with data: {data}")
    rand_num = random.randint(1, 100)
    emit_message(
        sid,
        "mission",
        {"message": f"What is the number between {rand_num} and {rand_num + 1}?"},
    )
    person_data = PersonData(f"Person{rand_num}", rand_num, f"{rand_num}@example.com")
    emit_message(sid, "data_transfer", person_data.to_dict())


# Emit a message to the client
def emit_message(sid, event, data):
    print(f"Sending {event} to {sid}: {data}")
    socketio_server.emit(event, data, room=sid)


# Parse query string into a dictionary
def parse_query(query):
    return dict(item.split("=") for item in query.split("&") if "=" in item)


# Data class for person information
class PersonData:
    def __init__(self, name, age, email):
        self.name = name
        self.age = age
        self.email = email

    def to_dict(self):
        return {"name": self.name, "age": self.age, "email": self.email}


# Create WSGI app and start the server
app = socketio.WSGIApp(socketio_server)
if __name__ == "__main__":
    port = 11100
    print(f"Listening on *:{port}")
    eventlet.wsgi.server(eventlet.listen(("", port)), app)
