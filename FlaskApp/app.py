from flask import Flask, render_template, Blueprint
import controllers

app = Flask(__name__)
login = Blueprint('login', __name__)

app.register_blueprint(controllers.login, url_prefix='/login')

@app.route("/")
def main():
	return render_template('index.html')




if __name__ == "__main__":
	app.run()
