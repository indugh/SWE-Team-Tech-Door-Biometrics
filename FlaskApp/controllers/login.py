from flask import render_template, flash, redirect
from forms import LoginForm, RegistrationForm
from flask import *
import sqlite3

sqlite_file = 'database.db'
table_name = 'users'
username_column = 'username'
pword_column = 'password'

DUO_IKEY = 'DIFRGMA77D2LMZYAESSU'
DUO_SKEY = '3PaauXz74LrY9l7aEVxdrbeP8IryyNhToMBemr3I'
DUO_AKEY = '4ff66a503b06879cd2a1e565ba956751ad885a22'
DUO_HOST = 'api-255d7c3c.duosecurity.com'

ikey = DUO_IKEY
skey = DUO_SKEY
akey = DUO_AKEY

from duo_web import sign_request, verify_response

conn = sqlite3.connect(sqlite_file, check_same_thread=False)
c = conn.cursor()

login = Blueprint('login', __name__)

@login.route('/login', methods = ['GET', 'POST'])
def login_route():
	if request.method == 'GET':
		return render_template('login.html')
	if request.method == 'POST':
		username = request.form['username']
		password = request.form['password']
		print "hi"
		print username
		print password
		c.execute("SELECT username, password FROM users where username=? and password=?", (username, password))
		user = c.fetchone()
		sig_request = sign_request('DIFRGMA77D2LMZYAESSU', '3PaauXz74LrY9l7aEVxdrbeP8IryyNhToMBemr3I', akey, username)
		return render_template('duo_login.html', sig_request = sig_request, post_action = request.path)
	return render_template('home.html')

@login.route('/duo_login', methods = ['GET' ,'POST'])
def duo_login_route():
	return render_template('duo_login.html')

@login.route('/register', methods = ['GET', 'POST'])
def register_route():
	if request.method == "GET":
		return render_template('register.html')
	elif request.method == "POST":
		username = request.form['username']
		password = request.form['password']
		sig_request = sign_request('DIFRGMA77D2LMZYAESSU', '3PaauXz74LrY9l7aEVxdrbeP8IryyNhToMBemr3I', akey, username)
		c.execute("INSERT INTO users (username, password) values (?,?)",(username,password))
		conn.commit()
		return render_template('duo_login.html', sig_request = sig_request, post_action = request.path)
		return redirect('/home')

	
@login.route('/home', methods = ['GET','POST'])
def home_route():
	if request.method == "POST":
		sig_response = request.args.get("sig_response") # for example (if using Tornado: http://www.tornadoweb.org/en/stable/documentation.html)
		authenticated_username = verify_response(ikey, skey, akey, sig_response)
		if authenticated_username:
			print "yay"
  			log_user_in(authenticated_username)
	return render_template('index.html')


