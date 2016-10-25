from flask import Flask, render_template, Blueprint
import sqlite3
import controllers

app = Flask(__name__)
app.debug = True
app.secret_key = '\xb2,\xe1\x06\xb1\x0f*X\x18\x9e\xf3\xa9`\x13\x05\xfcb\x9e#W\xea\xb6\xa4Q'

conn = sqlite3.connect('database.db')
print "Opened database successfully"

#conn.execute('CREATE TABLE users (username TEXT, password TEXT)')
print "Table created successfully"
conn.close()

##login = Blueprint('login', __name__)

app.register_blueprint(controllers.login)

@app.route("/")
def main():
	return render_template('index.html')

if __name__ == "__main__":
	app.run()

