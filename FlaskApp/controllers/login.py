from flask import *

login = Blueprint('login', __name__)

@login.route('/login', methods = ['GET', 'POST'])
def login_route():
	error_username = False
	error_password = False
	options = {
		"err_username" : error_username,
		"err_password" : error_password
	#	"username" : session['username']
	}
	return render_template('login.html', **options)

